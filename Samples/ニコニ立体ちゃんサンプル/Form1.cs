﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ニコニ立体ちゃんサンプル
{
    public partial class Form1 : SharpDX.Windows.RenderForm
    {
        public Form1()
        {
            InitializeComponent();
        }

        // アプリの初期化
        protected override void OnLoad( EventArgs e )
        {
            this.ClientSize = new Size( 1280, 720 );
            this.Text = "ニコニ立体ちゃんサンプル for MikuMikuFlex 3";


            // Direct3D11 を初期化します。

            this._Direct3Dを初期化する();


            // シーンを作成します。
            {
                // 既定の深度ステンシルと、既定のレンダーターゲット（１つ）を、それぞれ（ビューではなく）リソースで渡します。
                this._シーン = new MikuMikuFlex3.シーン( this._D3D11Device, this._既定のD3D11DepthStencil, this._既定のD3D11RenderTarget );
            }

            // モデル１・アリシアを作成してシーンに追加します。
            {
                // PMXファイルを指定してモデルを生成します。
                this._モデル = new MikuMikuFlex3.PMXモデル( this._D3D11Device, @"サンプルデータ/Alicia/MMD/Alicia_solid.pmx" );

                // 各材質ごとに持つテッセレーションの値（初期値 1）を、5 に変更します（任意）。
                foreach( var mat in this._モデル.材質リスト )
                    mat.テッセレーション係数 = 5;

                // VMDファイルを指定して、PMXモデルにアニメーションを追加します。
                MikuMikuFlex3.VMDアニメーションビルダ.アニメーションを追加する( @"サンプルデータ/Alicia/MMD Motion/2分ループステップ1.vmd", this._モデル );

                // モデルをシーンに追加します。
                this._シーン.追加する( this._モデル );
            }

            // モデル２・ニコニ立体ステージを作成してシーンに追加します。
            {
                // PMXファイルを指定してモデルを生成します。
                this._ステージ = new MikuMikuFlex3.PMXモデル( this._D3D11Device, @"サンプルデータ/nicosolid_stage/nicosolid_stage.pmx" );

                // モデルをシーンに追加します。
                this._シーン.追加する( this._ステージ );
            }

            // カメラを作成してシーンに追加します。
            {
                // いくつかあるカメラのうち、ここでは、マウスモーションカメラを作成します。
                this._カメラ = new MikuMikuFlex3.マウスモーションカメラ( 45f );
                this.MouseDown += this._カメラ.OnMouseDown;
                this.MouseUp += this._カメラ.OnMouseUp;
                this.MouseMove += this._カメラ.OnMouseMove;
                this.MouseWheel += this._カメラ.OnMouseWheel;

                // カメラをシーンに追加します。
                this._シーン.追加する( this._カメラ );
            }

            // 照明を作成してシーンに追加します。
            {
                // 照明を作成します。
                this._照明 = new MikuMikuFlex3.照明();

                // 照明をシーンに追加します。
                this._シーン.追加する( this._照明 );
            }

            // 完了。

            this.Activate(); // ウィンドウが後ろに隠れることがあるので、念のため。
            base.OnLoad( e );


            // 処理のごたごたが落ち着いたらメインループへ。
            Application.Idle += this.Run;
        }

        // アプリのメインループ
        private void Run( object sender, EventArgs e )
        {
            Application.Idle -= this.Run;   // イベントが複数発生する場合があるのでそれを回避。

            // シーンの描画には「現在時刻」が必要であるため、タイマを準備します。
            var timer = Stopwatch.StartNew();

            // FPSを計測するクラスを生成します（任意）。
            var fps = new MikuMikuFlex3.Utility.FPS();

            // メインループを開始します。
            SharpDX.Windows.RenderLoop.Run( this, () => {


                // FPS を計測し、デバッグ表示します（任意）。

                if( fps.FPSをカウントする() )
                    Debug.WriteLine( $"{fps.現在のFPS} fps" );


                // 既定のレンダーターゲットと既定の深度ステンシルをクリアします。

                var 背景色 = new SharpDX.Color4( 0.2f, 0.4f, 0.8f, 1.0f );
                this._D3D11Device.ImmediateContext.ClearRenderTargetView( this._既定のD3D11RenderTargetView, 背景色 );
                this._D3D11Device.ImmediateContext.ClearDepthStencilView( this._既定のD3D11DepthStencilView, SharpDX.Direct3D11.DepthStencilClearFlags.Depth | SharpDX.Direct3D11.DepthStencilClearFlags.Stencil, 1f, 0 );


                // 時刻を指定して、シーンを描画します。

                var 現在時刻sec = timer.ElapsedMilliseconds / 1000.0;
                this._シーン.描画する( 現在時刻sec, this._D3D11Device.ImmediateContext );


                // スワップチェーンを表示します。

                this._DXGISwapChain.Present( 0, SharpDX.DXGI.PresentFlags.None );

            } );
        }

        // アプリの終了
        protected override void OnClosing( CancelEventArgs e )
        {
            this._モデル?.Dispose();
            this._照明 = null;
            this._カメラ = null;
            this._シーン?.Dispose();

            this._Direct3Dを解放する();

            base.OnClosing( e );
        }


        // MikuMikuFlex 3

        private MikuMikuFlex3.シーン _シーン;

        private MikuMikuFlex3.マウスモーションカメラ _カメラ;

        private MikuMikuFlex3.照明 _照明;

        private MikuMikuFlex3.PMXモデル _モデル;

        private MikuMikuFlex3.PMXモデル _ステージ;


        // Direct3D11

        private SharpDX.Direct3D11.Device _D3D11Device;

        private SharpDX.DXGI.SwapChain _DXGISwapChain;

        private SharpDX.Direct3D11.Texture2D _既定のD3D11RenderTarget;

        private SharpDX.Direct3D11.RenderTargetView _既定のD3D11RenderTargetView;

        private SharpDX.Direct3D11.Texture2D _既定のD3D11DepthStencil;

        private SharpDX.Direct3D11.DepthStencilView _既定のD3D11DepthStencilView;

        private SharpDX.Direct3D11.BlendState _BlendState加算合成;

        private SharpDX.Direct3D11.RasterizerState _RasterizerState;


        private void _Direct3Dを初期化する()
        {
            // D3D11デバイスと SwapChain を生成します。

            SharpDX.Direct3D11.Device.CreateWithSwapChain(
                SharpDX.Direct3D.DriverType.Hardware,
                SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport,
                new SharpDX.Direct3D.FeatureLevel[] { SharpDX.Direct3D.FeatureLevel.Level_11_1 },   // 機能レベル 11.1
                new SharpDX.DXGI.SwapChainDescription {
                    BufferCount = 1,
                    Flags = SharpDX.DXGI.SwapChainFlags.AllowModeSwitch,
                    IsWindowed = true,
                    ModeDescription = new SharpDX.DXGI.ModeDescription {
                        Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                        Width = this.ClientSize.Width,
                        Height = this.ClientSize.Height,
                        Scaling = SharpDX.DXGI.DisplayModeScaling.Stretched,
                    },
                    OutputHandle = this.Handle,
                    SampleDescription = new SharpDX.DXGI.SampleDescription( 4, 0 ), // MSAA x4
                    SwapEffect = SharpDX.DXGI.SwapEffect.Discard,
                    Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                },
                out this._D3D11Device,
                out this._DXGISwapChain );


            // 既定のRenderTarget と 既定のDepthStencil ならびにそのビューを作成します。

            this._既定のD3D11RenderTarget = this._DXGISwapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>( 0 );

            this._既定のD3D11DepthStencil = new SharpDX.Direct3D11.Texture2D(
                this._D3D11Device,
                new SharpDX.Direct3D11.Texture2DDescription {
                    Width = this._既定のD3D11RenderTarget.Description.Width,
                    Height = this._既定のD3D11RenderTarget.Description.Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt,
                    SampleDescription = this._既定のD3D11RenderTarget.Description.SampleDescription,
                    Usage = SharpDX.Direct3D11.ResourceUsage.Default,
                    BindFlags = SharpDX.Direct3D11.BindFlags.DepthStencil,
                    CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                    OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
                } );

            this._既定のD3D11RenderTargetView = new SharpDX.Direct3D11.RenderTargetView( this._D3D11Device, this._既定のD3D11RenderTarget );

            this._既定のD3D11DepthStencilView = new SharpDX.Direct3D11.DepthStencilView( this._D3D11Device, this._既定のD3D11DepthStencil );



            // 加算合成用のブレンドステートを作成します。

            var BlendStateAdd = new SharpDX.Direct3D11.BlendStateDescription() {
                AlphaToCoverageEnable = false,  // アルファマスクで透過する（するならZバッファ必須）
                IndependentBlendEnable = false, // 個別設定。false なら BendStateDescription.RenderTarget[0] だけが有効で、[1～7] は無視される。
            };
            BlendStateAdd.RenderTarget[ 0 ].IsBlendEnabled = true; // true ならブレンディングが有効。
            BlendStateAdd.RenderTarget[ 0 ].RenderTargetWriteMask = SharpDX.Direct3D11.ColorWriteMaskFlags.All;        // RGBA の書き込みマスク。
            // アルファ値のブレンディング設定 ... 特になし
            BlendStateAdd.RenderTarget[ 0 ].SourceAlphaBlend = SharpDX.Direct3D11.BlendOption.One;
            BlendStateAdd.RenderTarget[ 0 ].DestinationAlphaBlend = SharpDX.Direct3D11.BlendOption.Zero;
            BlendStateAdd.RenderTarget[ 0 ].AlphaBlendOperation = SharpDX.Direct3D11.BlendOperation.Add;
            // 色値のブレンディング設定 ... 加算合成
            BlendStateAdd.RenderTarget[ 0 ].SourceBlend = SharpDX.Direct3D11.BlendOption.SourceAlpha;
            BlendStateAdd.RenderTarget[ 0 ].DestinationBlend = SharpDX.Direct3D11.BlendOption.One;
            BlendStateAdd.RenderTarget[ 0 ].BlendOperation = SharpDX.Direct3D11.BlendOperation.Add;
            // ブレンドステートを作成する。
            this._BlendState加算合成 = new SharpDX.Direct3D11.BlendState( this._D3D11Device, BlendStateAdd );


            // ブレンドステートと深度ステンシルステートを OM に設定します。

            this._D3D11Device.ImmediateContext.OutputMerger.BlendState = this._BlendState加算合成;
            this._D3D11Device.ImmediateContext.OutputMerger.DepthStencilState = null;


            // ラスタライザステートを作成します。

            this._RasterizerState = new SharpDX.Direct3D11.RasterizerState(
                this._D3D11Device,
                new SharpDX.Direct3D11.RasterizerStateDescription {
                    CullMode = SharpDX.Direct3D11.CullMode.Back,
                    FillMode = SharpDX.Direct3D11.FillMode.Solid,
                } );
        }

        private void _Direct3Dを解放する()
        {
            this._RasterizerState?.Dispose();
            this._BlendState加算合成?.Dispose();

            this._既定のD3D11DepthStencilView?.Dispose();
            this._既定のD3D11DepthStencil?.Dispose();

            this._既定のD3D11RenderTargetView?.Dispose();
            this._既定のD3D11RenderTarget?.Dispose();

            this._DXGISwapChain?.Dispose();

            this._D3D11Device?.Dispose();
        }
    }
}
