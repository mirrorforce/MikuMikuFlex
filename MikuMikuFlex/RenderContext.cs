﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MMF.DeviceManager;
using MMF.ライト;
using MMF.行列;
using MMF.モデル;
using MMF.モーション;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;

namespace MMF
{
	/// <summary>
	///     レンダリングに関する様々な情報を保持します。
	///     3DCG描画をする際はこの参照が必要になることが多いです
	/// </summary>
	public class RenderContext : IDisposable
	{
        // シングルトン

        public static RenderContext Instance
        {
            get;
            protected set;
        } = null;

        public static void インスタンスを生成する()
        {
            Instance = new RenderContext();
            Instance.Initialize();
        }
        public static void インスタンスを生成する( IDeviceManager deviceManager )
        {
            Instance = new RenderContext();
            Instance.Initialize( deviceManager );
        }
        public static void インスタンスを解放する()
        {
            Instance?.Dispose();
            Instance = null;
        }


        // イベント

        public event EventHandler 更新通知 = delegate { };


        // プロパティ

        public IDeviceManager DeviceManager { get; private set; }

        /// <summary>
        ///     一定時間ごとに <see cref="ワールド座標をすべて更新する"/> を呼び出すタイマ。
        /// </summary>
        public モーションタイマ Timer;

        /// <summary>
        ///     レンダーターゲットのクリア色。
        ///     スクリプトの Clear 関数で使われる。
        /// </summary>
		public Color4 クリア色 { get; set; }

        /// <summary>
        ///     深度ステンシルのクリア色。
        ///     スクリプトの Clear 関数で使われる。
        /// </summary>
		public float クリア深度 { get; set; }

        /// <summary>
        ///     マウスの挙動をリアルタイム取得。
        ///		ScreenContextがターゲットになっていないとnullになる。
        /// </summary>
        public マウス監視 パネル監視;

        /// <summary>
        ///     コントロールとScreenContestのマップ。
        /// </summary>
        public Dictionary<Control, ScreenContext> ControltoScreenContextマップ { get; } = new Dictionary<Control, ScreenContext>();


        // 描画先プロパティ

        public List<ScreenContext> ScreenContextリスト = new List<ScreenContext>();

        /// <summary>
        ///     描画先のリソース一式。
        ///     レンダーターゲット、深度ステンシル、行列、カメラ、ビューポート、スワップチェーンなど。
        /// </summary>
		public ITargetContext 描画ターゲットコンテキスト  { get; private set; }

        public RenderTargetView[] レンダーターゲット配列 = new RenderTargetView[ 8 ];

        public DepthStencilView 深度ステンシルターゲット;


        // 描画ステートプロパティ

        public 照明行列管理 照明行列管理;

        public 行列管理 行列管理 => 描画ターゲットコンテキスト.行列管理;

        /// <summary>
        ///     任意の DeviceContext に BlendState を設定できる。
        /// </summary>
		public ブレンドステート管理 ブレンドステート管理 { get; private set; }

        public RasterizerState 片面描画の際のラスタライザステート { get; private set; }

        public RasterizerState 両面描画の際のラスタライザステート { get; private set; }


        // メソッド

        protected RenderContext()
		{
            if( null != Instance )
                throw new Exception( "インスタンスはすでに生成済みです。" );
        }

        public void Initialize()
		{
			this._デバイスを初期化する();

            this.Timer = new モーションタイマ();

            this.ブレンドステート管理 = new ブレンドステート管理( this );
            this.ブレンドステート管理.ブレンドステートを設定する( this.DeviceManager.D3DDeviceContext, ブレンドステート管理.BlendStates.Alignment );

        }

        public void Initialize( IDeviceManager deviceManager )
        {
            this.DeviceManager = deviceManager;
            this.Initialize();
        }

        public ScreenContext Initialize( Control targetControl )
		{
            this.Initialize();

			var matrixManager = this._行列を初期化する();
			var primaryContext = new ScreenContext( targetControl, matrixManager );
			this.ControltoScreenContextマップ.Add( targetControl, primaryContext );

            this.描画ターゲットコンテキスト = primaryContext;
            this._レンダーターゲットを更新する();

			return primaryContext;
		}

        public void Dispose()
        {
            foreach( var screenContext in this.ControltoScreenContextマップ )
                screenContext.Value.Dispose();

            this.片面描画の際のラスタライザステート?.Dispose();
            this.片面描画の際のラスタライザステート = null;

            this.両面描画の際のラスタライザステート?.Dispose();
            this.両面描画の際のラスタライザステート = null;

            foreach( var disposable in this.Disposables )
                disposable.Dispose();

            if( this._DeviceManagerの破棄をこのインスタンスで行う )
                DeviceManager.Dispose();
        }

        public void 画面をクリアする( Color4 color )
		{
			this._レンダーターゲットを更新する();

			this.DeviceManager.D3DDeviceContext.ClearRenderTargetView( this.描画ターゲットコンテキスト.D3Dレンダーターゲットビュー, color );
			this.DeviceManager.D3DDeviceContext.ClearDepthStencilView( this.描画ターゲットコンテキスト.深度ステンシルビュー, DepthStencilClearFlags.Depth, 1, 0 );
		}

		public void ワールド座標をすべて更新する( ScreenContext screen )
        {
            screen.ワールド空間.すべてのDynamicTextureを更新する();
            screen.ワールド空間.すべてのDrawableGroupを更新する();

			// 自身も更新。
			this.更新通知( this, new EventArgs() );
		}

		public void 描画対象にする( ITargetContext context )
		{
			this.描画ターゲットコンテキスト = context;
			context.ビューポートを設定する();

            _レンダーターゲットを更新する();
		}

		public ScreenContext ScreenContextを作成する( Control control )
		{
			var カメラ = new カメラ(
				カメラの初期位置: new Vector3( 0, 20, -40 ),
				カメラの初期注視点: new Vector3( 0, 3, 0 ), 
				カメラの初期上方向ベクトル: new Vector3( 0, 1, 0 ) );

			var 射影行列 = new 射影();
			射影行列.射影行列を初期化する( (float) Math.PI / 4f, 1.618f, 1, 200 );

			var matrixManager = new 行列管理( new ワールド行列(), カメラ, 射影行列 );

			var context = new ScreenContext( control, matrixManager );
			this.ControltoScreenContextマップ.Add( control, context );

			return context;
		}

		public Texture2D Texture2Dを作成する( Texture2DDescription desc )
		{
			return new Texture2D( DeviceManager.D3DDevice, desc );
		}


		internal List<IDisposable> Disposables = new List<IDisposable>();

        private bool _DeviceManagerの破棄をこのインスタンスで行う = false;


		private 行列管理 _行列を初期化する()
		{
			var カメラ = new カメラ(
				カメラの初期位置: new Vector3( 0, 20, -40 ),
				カメラの初期注視点: new Vector3( 0, 3, 0 ),
				カメラの初期上方向ベクトル: new Vector3( 0, 1, 0 ) );

			var 射影行列 = new 射影();
			射影行列.射影行列を初期化する( (float) Math.PI / 4f, 1.618f, 1, 2000 );

			var 行列管理 = new 行列管理( new ワールド行列(), カメラ, 射影行列 );

			this.照明行列管理 = new 照明行列管理( 行列管理 );

			return 行列管理;
		}

		private void _デバイスを初期化する()
		{
			this._行列を初期化する();

			// 未生成時の（コンストラクタで指定していない）ときのみ DeviceManager を生成する。
			if( this.DeviceManager == null )
			{
				this._DeviceManagerの破棄をこのインスタンスで行う = true;	// 自分が生成したので自分で破棄することを覚えておく。

				this.DeviceManager = new DeviceManager基本実装();
				this.DeviceManager.Load();
			}

			var desc片側 = new RasterizerStateDescription() {
				CullMode = SharpDX.Direct3D11.CullMode.Back,
				FillMode = SharpDX.Direct3D11.FillMode.Solid,
			};
			this.片面描画の際のラスタライザステート = new RasterizerState( DeviceManager.D3DDevice, desc片側 );

			var desc両側 = new RasterizerStateDescription() {
				CullMode = SharpDX.Direct3D11.CullMode.None,
				FillMode = SharpDX.Direct3D11.FillMode.Solid,
			};
			this.両面描画の際のラスタライザステート = new RasterizerState( DeviceManager.D3DDevice, desc両側 );
		}

		private void _レンダーターゲットを更新する()
		{
			this.レンダーターゲット配列[ 0 ] = this.描画ターゲットコンテキスト.D3Dレンダーターゲットビュー;
			this.深度ステンシルターゲット = this.描画ターゲットコンテキスト.深度ステンシルビュー;

			this.DeviceManager.D3DDeviceContext.OutputMerger.SetTargets(
				this.深度ステンシルターゲット,
				this.レンダーターゲット配列 );
		}
	}
}