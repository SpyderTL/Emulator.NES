using System;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Valve.VR;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace dotNES.Renderers
{
    class OpenVRRenderer : Control, IRenderer
    {
        private UI _ui;
        private readonly Object _drawLock = new Object();

		private CVRSystem system;
		private CVRCompositor compositor;
		private uint count;
		private uint headset;
		private List<uint> controllers;
		private RenderModel_t[] controllerModels;
		private RenderModel_TextureMap_t[] controllerTextures;
		private SharpDX.Direct3D11.Buffer[] controllerVertexBuffers;
		private SharpDX.Direct3D11.Buffer[] controllerIndexBuffers;
		private VertexBufferBinding[] controllerVertexBufferBindings;
		private TrackedDevicePose_t[] currentPoses;
		private TrackedDevicePose_t[] nextPoses;
		private ShaderResourceView[] controllerTextureViews;
		private Size windowSize;
		private Matrix leftEyeProjection;
		private Matrix rightEyeProjection;
		private Matrix leftEyeView;
		private Matrix rightEyeView;
		private RawColor4 backgroundColor;
		private Size headsetSize;
		private SharpDX.Direct3D11.Device device;
		private SwapChain swapChain;
		private SharpDX.Direct3D11.DeviceContext context;
		private RenderTargetView backBufferView;
		private DepthStencilView depthStencilView;
		private Shaders.Parameters shaderParameters;
		private SharpDX.Direct3D11.Buffer shaderParameterBuffer;
		private RasterizerState rasterizerState;
		private BlendState blendState;
		private DepthStencilState depthStencilState;
		private SamplerState samplerState;
		private DateTime startTime;
		private int frame;
		private Matrix head;
		private Texture2D leftEyeTexture;
		private Texture2D rightEyeTexture;
		private RenderTargetView leftEyeTextureView;
		private RenderTargetView rightEyeTextureView;
		private Texture2D eyeDepth;
		private DepthStencilView eyeDepthView;

		public string RendererName => "OpenVR";

		public void InitRendering(UI ui)
		{
			lock (_drawLock)
			{
				if (ui == null) return;
				_ui = ui;
				ResizeRedraw = true;

				var initError = EVRInitError.None;

				system = OpenVR.Init(ref initError);

				if (initError != EVRInitError.None)
					throw new Exception("Not Available");

				compositor = OpenVR.Compositor;

				compositor.CompositorBringToFront();
				compositor.FadeGrid(5.0f, false);

				count = OpenVR.k_unMaxTrackedDeviceCount;

				currentPoses = new TrackedDevicePose_t[count];
				nextPoses = new TrackedDevicePose_t[count];

				controllers = new List<uint>();
				controllerModels = new RenderModel_t[count];
				controllerTextures = new RenderModel_TextureMap_t[count];
				controllerTextureViews = new ShaderResourceView[count];
				controllerVertexBuffers = new SharpDX.Direct3D11.Buffer[count];
				controllerIndexBuffers = new SharpDX.Direct3D11.Buffer[count];
				controllerVertexBufferBindings = new VertexBufferBinding[count];

				for (uint device = 0; device < count; device++)
				{
					var deviceClass = system.GetTrackedDeviceClass(device);

					switch (deviceClass)
					{
						case ETrackedDeviceClass.HMD:
							headset = device;
							break;

						case ETrackedDeviceClass.Controller:
							controllers.Add(device);
							break;
					}
				}

				uint width = 0;
				uint height = 0;

				system.GetRecommendedRenderTargetSize(ref width, ref height);

				headsetSize = new Size((int)width, (int)height);
				windowSize = new Size(UI.GameWidth, UI.GameHeight);

				leftEyeProjection = Convert(system.GetProjectionMatrix(EVREye.Eye_Left, 0.01f, 1000.0f));
				rightEyeProjection = Convert(system.GetProjectionMatrix(EVREye.Eye_Right, 0.01f, 1000.0f));

				leftEyeView = Convert(system.GetEyeToHeadTransform(EVREye.Eye_Left));
				rightEyeView = Convert(system.GetEyeToHeadTransform(EVREye.Eye_Right));

				foreach (var controller in controllers)
				{
					var modelName = new StringBuilder(255, 255);
					var propertyError = ETrackedPropertyError.TrackedProp_Success;

					var length = system.GetStringTrackedDeviceProperty(controller, ETrackedDeviceProperty.Prop_RenderModelName_String, modelName, 255, ref propertyError);

					if (propertyError == ETrackedPropertyError.TrackedProp_Success)
					{
						var modelName2 = modelName.ToString();

						while (true)
						{
							var pointer = IntPtr.Zero;
							var modelError = EVRRenderModelError.None;

							modelError = OpenVR.RenderModels.LoadRenderModel_Async(modelName2, ref pointer);

							if (modelError == EVRRenderModelError.Loading)
								continue;

							if (modelError == EVRRenderModelError.None)
							{
								var renderModel = System.Runtime.InteropServices.Marshal.PtrToStructure<RenderModel_t>(pointer);

								controllerModels[controller] = renderModel;
								break;
							}
						}

						while (true)
						{
							var pointer = IntPtr.Zero;
							var textureError = EVRRenderModelError.None;

							textureError = OpenVR.RenderModels.LoadTexture_Async(controllerModels[controller].diffuseTextureId, ref pointer);

							if (textureError == EVRRenderModelError.Loading)
								continue;

							if (textureError == EVRRenderModelError.None)
							{
								var texture = System.Runtime.InteropServices.Marshal.PtrToStructure<RenderModel_TextureMap_t>(pointer);

								controllerTextures[controller] = texture;
								break;
							}
						}
					}
				}

				int adapterIndex = 0;

				system.GetDXGIOutputInfo(ref adapterIndex);

				using (var factory = new SharpDX.DXGI.Factory4())
				{
					var adapter = factory.GetAdapter(adapterIndex);

					var swapChainDescription = new SwapChainDescription
					{
						BufferCount = 1,
						Flags = SwapChainFlags.None,
						IsWindowed = true,
						ModeDescription = new ModeDescription
						{
							Format = Format.B8G8R8A8_UNorm,
							Width = windowSize.Width,
							Height = windowSize.Height,
							RefreshRate = new Rational(60, 1)
						},
						OutputHandle = this.Handle,
						SampleDescription = new SampleDescription(1, 0),
						SwapEffect = SwapEffect.Discard,
						Usage = Usage.RenderTargetOutput
					};

					SharpDX.Direct3D11.Device.CreateWithSwapChain(adapter, DeviceCreationFlags.None, swapChainDescription, out device, out swapChain);

					factory.MakeWindowAssociation(this.Handle, WindowAssociationFlags.None);

					context = device.ImmediateContext;

					using (var backBuffer = swapChain.GetBackBuffer<Texture2D>(0))
						backBufferView = new RenderTargetView(device, backBuffer);

					var depthBufferDescription = new Texture2DDescription
					{
						Format = Format.D16_UNorm,
						ArraySize = 1,
						MipLevels = 1,
						Width = windowSize.Width,
						Height = windowSize.Height,
						SampleDescription = new SampleDescription(1, 0),
						Usage = ResourceUsage.Default,
						BindFlags = BindFlags.DepthStencil,
						CpuAccessFlags = CpuAccessFlags.None,
						OptionFlags = ResourceOptionFlags.None
					};

					using (var depthBuffer = new Texture2D(device, depthBufferDescription))
						depthStencilView = new DepthStencilView(device, depthBuffer);

					// Create Eye Textures
					var eyeTextureDescription = new Texture2DDescription
					{
						ArraySize = 1,
						BindFlags = BindFlags.RenderTarget,
						CpuAccessFlags = CpuAccessFlags.None,
						Format = Format.B8G8R8A8_UNorm,
						Width = headsetSize.Width,
						Height = headsetSize.Height,
						MipLevels = 1,
						OptionFlags = ResourceOptionFlags.None,
						SampleDescription = new SampleDescription(1, 0),
						Usage = ResourceUsage.Default
					};

					leftEyeTexture = new Texture2D(device, eyeTextureDescription);
					rightEyeTexture = new Texture2D(device, eyeTextureDescription);

					leftEyeTextureView = new RenderTargetView(device, leftEyeTexture);
					rightEyeTextureView = new RenderTargetView(device, rightEyeTexture);

					// Create Eye Depth Buffer
					eyeTextureDescription.BindFlags = BindFlags.DepthStencil;
					eyeTextureDescription.Format = Format.D32_Float;

					eyeDepth = new Texture2D(device, eyeTextureDescription);
					eyeDepthView = new DepthStencilView(device, eyeDepth);

					Shapes.Cube.Load(device);
					Shapes.Sphere.Load(device);
					Shaders.Normal.Load(device);
					Shaders.NormalTexture.Load(device);

					// Load Controller Models
					foreach (var controller in controllers)
					{
						var model = controllerModels[controller];

						controllerVertexBuffers[controller] = new SharpDX.Direct3D11.Buffer(device, model.rVertexData, new BufferDescription
						{
							BindFlags = BindFlags.VertexBuffer,
							SizeInBytes = (int)model.unVertexCount * 32
						});

						controllerVertexBufferBindings[controller] = new VertexBufferBinding(controllerVertexBuffers[controller], 32, 0);

						controllerIndexBuffers[controller] = new SharpDX.Direct3D11.Buffer(device, model.rIndexData, new BufferDescription
						{
							BindFlags = BindFlags.IndexBuffer,
							SizeInBytes = (int)model.unTriangleCount * 3 * 2
						});

						var texture = controllerTextures[controller];

						using (var texture2d = new Texture2D(device, new Texture2DDescription
						{
							ArraySize = 1,
							BindFlags = BindFlags.ShaderResource,
							Format = Format.R8G8B8A8_UNorm,
							Width = texture.unWidth,
							Height = texture.unHeight,
							MipLevels = 1,
							SampleDescription = new SampleDescription(1, 0)
						}, new DataRectangle(texture.rubTextureMapData, texture.unWidth * 4)))
							controllerTextureViews[controller] = new ShaderResourceView(device, texture2d);
					}

					shaderParameterBuffer = new SharpDX.Direct3D11.Buffer(device, Utilities.SizeOf<Shaders.Parameters>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

					var rasterizerStateDescription = RasterizerStateDescription.Default();
					//rasterizerStateDescription.FillMode = FillMode.Wireframe;
					rasterizerStateDescription.IsFrontCounterClockwise = true;
					//rasterizerStateDescription.CullMode = CullMode.None;

					rasterizerState = new RasterizerState(device, rasterizerStateDescription);

					var blendStateDescription = BlendStateDescription.Default();

					blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
					blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
					blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;

					blendStateDescription.RenderTarget[0].IsBlendEnabled = false;

					blendState = new BlendState(device, blendStateDescription);

					var depthStateDescription = DepthStencilStateDescription.Default();

					depthStateDescription.DepthComparison = Comparison.LessEqual;
					depthStateDescription.IsDepthEnabled = true;
					depthStateDescription.IsStencilEnabled = false;

					depthStencilState = new DepthStencilState(device, depthStateDescription);

					var samplerStateDescription = SamplerStateDescription.Default();

					samplerStateDescription.Filter = Filter.MinMagMipLinear;
					samplerStateDescription.AddressU = TextureAddressMode.Wrap;
					samplerStateDescription.AddressV = TextureAddressMode.Wrap;

					samplerState = new SamplerState(device, samplerStateDescription);

					startTime = DateTime.Now;
					frame = 0;
					//windowSize = ClientSize;

					backgroundColor = new RawColor4(0.1f, 0.1f, 0.1f, 1);

					head = Matrix.Identity;

					_ui.ready = true;
				}
			}
		}

        public void EndRendering()
        {
            DisposeDirect3D();
        }

        private void DisposeDirect3D()
        {
            lock (_drawLock)
            {
                if (_ui != null && _ui.ready)
                {
                    _ui.ready = false;
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            try
            {
                DisposeDirect3D();
                InitRendering(_ui);
                base.OnResize(e);
            }
            catch
            {
                // This is pretty stupid, but Mono will send a resize event to this component
                // even when it's not added to a frame, so this will fail horribly
                // during the renderer self-test procedure, which detects this type of failure...
                // on different thread.
            }
        }

        public void Draw()
        {
            lock (_drawLock)
            {
                if (_ui == null || !_ui.ready) return;

				if (_ui.gameStarted)
                {
					// Update Device Tracking
					compositor.WaitGetPoses(currentPoses, nextPoses);

					if (currentPoses[headset].bPoseIsValid)
					{
						Convert(ref currentPoses[headset].mDeviceToAbsoluteTracking, ref head);
					}

					// Render Left Eye
					context.Rasterizer.SetViewport(0, 0, headsetSize.Width, headsetSize.Height);
					context.OutputMerger.SetTargets(eyeDepthView, leftEyeTextureView);
					context.OutputMerger.SetDepthStencilState(depthStencilState);

					context.ClearRenderTargetView(leftEyeTextureView, backgroundColor);
					context.ClearDepthStencilView(eyeDepthView, DepthStencilClearFlags.Depth, 1.0f, 0);

					Shaders.Normal.Apply(context);

					context.Rasterizer.State = rasterizerState;

					context.OutputMerger.SetBlendState(blendState);
					context.OutputMerger.SetDepthStencilState(depthStencilState);

					context.PixelShader.SetSampler(0, samplerState);

					var ratio = (float)headsetSize.Width / (float)headsetSize.Height;

					var projection = leftEyeProjection;
					var view = Matrix.Invert(leftEyeView * head);
					var world = Matrix.Translation(0, 0, -100.0f);

					var worldViewProjection = world * view * projection;

					//context.UpdateSubresource(ref worldViewProjection, worldViewProjectionBuffer);

					context.VertexShader.SetConstantBuffer(0, shaderParameterBuffer);
					context.PixelShader.SetConstantBuffer(0, shaderParameterBuffer);

					//Shapes.Cube.Begin(context);
					//Shapes.Cube.Draw(context);

					//Shapes.Sphere.Begin(context);
					//Shapes.Sphere.Draw(context);

					DrawPixels(worldViewProjection);

					// Draw Controllers
					context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

					Shaders.NormalTexture.Apply(context);

					context.PixelShader.SetSampler(0, samplerState);

					foreach (var controller in controllers)
					{
						context.InputAssembler.SetVertexBuffers(0, controllerVertexBufferBindings[controller]);
						context.InputAssembler.SetIndexBuffer(controllerIndexBuffers[controller], Format.R16_UInt, 0);

						context.PixelShader.SetShaderResource(0, controllerTextureViews[controller]);

						Convert(ref currentPoses[controller].mDeviceToAbsoluteTracking, ref world);

						shaderParameters.WorldViewProjection = world * view * projection;
						shaderParameters.Diffuse = new Vector4(1, 1, 1, 1);

						context.UpdateSubresource(ref shaderParameters, shaderParameterBuffer);

						context.VertexShader.SetConstantBuffer(0, shaderParameterBuffer);
						context.PixelShader.SetConstantBuffer(0, shaderParameterBuffer);

						context.DrawIndexed((int)controllerModels[controller].unTriangleCount * 3 * 4, 0, 0);
					}

					var texture = new Texture_t
					{
						eType = ETextureType.DirectX,
						eColorSpace = EColorSpace.Gamma,
						handle = leftEyeTextureView.Resource.NativePointer
					};

					var bounds = new VRTextureBounds_t
					{
						uMin = 0.0f,
						uMax = 1.0f,
						vMin = 0.0f,
						vMax = 1.0f,
					};

					var submitError = compositor.Submit(EVREye.Eye_Left, ref texture, ref bounds, EVRSubmitFlags.Submit_Default);

					if (submitError != EVRCompositorError.None)
						System.Diagnostics.Debug.WriteLine(submitError);

					// Render Right Eye
					context.Rasterizer.SetViewport(0, 0, headsetSize.Width, headsetSize.Height);
					context.OutputMerger.SetTargets(eyeDepthView, rightEyeTextureView);
					context.OutputMerger.SetDepthStencilState(depthStencilState);

					context.ClearRenderTargetView(rightEyeTextureView, backgroundColor);
					context.ClearDepthStencilView(eyeDepthView, DepthStencilClearFlags.Depth, 1.0f, 0);

					Shaders.Normal.Apply(context);

					context.Rasterizer.State = rasterizerState;

					context.OutputMerger.SetBlendState(blendState);
					context.OutputMerger.SetDepthStencilState(depthStencilState);

					context.PixelShader.SetSampler(0, samplerState);

					projection = rightEyeProjection;
					view = Matrix.Invert(rightEyeView * head);
					world = Matrix.Translation(0, 0, -100.0f);

					worldViewProjection = world * view * projection;

					//context.UpdateSubresource(ref worldViewProjection, worldViewProjectionBuffer);

					context.VertexShader.SetConstantBuffer(0, shaderParameterBuffer);
					context.PixelShader.SetConstantBuffer(0, shaderParameterBuffer);

					//Shapes.Cube.Begin(context);
					//Shapes.Cube.Draw(context);

					//Shapes.Sphere.Begin(context);
					//Shapes.Sphere.Draw(context);

					DrawPixels(worldViewProjection);

					// Draw Controllers
					context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

					Shaders.NormalTexture.Apply(context);

					context.PixelShader.SetSampler(0, samplerState);

					foreach (var controller in controllers)
					{
						context.InputAssembler.SetVertexBuffers(0, controllerVertexBufferBindings[controller]);
						context.InputAssembler.SetIndexBuffer(controllerIndexBuffers[controller], Format.R16_UInt, 0);

						context.PixelShader.SetShaderResource(0, controllerTextureViews[controller]);

						Convert(ref currentPoses[controller].mDeviceToAbsoluteTracking, ref world);

						shaderParameters.WorldViewProjection = world * view * projection;
						shaderParameters.Diffuse = new Vector4(1, 1, 1, 1);

						context.UpdateSubresource(ref shaderParameters, shaderParameterBuffer);

						context.VertexShader.SetConstantBuffer(0, shaderParameterBuffer);
						context.PixelShader.SetConstantBuffer(0, shaderParameterBuffer);

						context.DrawIndexed((int)controllerModels[controller].unTriangleCount * 3 * 4, 0, 0);
					}

					texture.handle = rightEyeTextureView.Resource.NativePointer;

					submitError = compositor.Submit(EVREye.Eye_Right, ref texture, ref bounds, EVRSubmitFlags.Submit_Default);

					if (submitError != EVRCompositorError.None)
						System.Diagnostics.Debug.WriteLine(submitError);

					// Render Window
					context.Rasterizer.SetViewport(0, 0, windowSize.Width, windowSize.Height);

					context.OutputMerger.SetTargets(depthStencilView, backBufferView);
					context.OutputMerger.SetDepthStencilState(depthStencilState);

					context.ClearRenderTargetView(backBufferView, backgroundColor);
					context.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

					Shaders.Normal.Apply(context);

					context.Rasterizer.State = rasterizerState;

					context.OutputMerger.SetBlendState(blendState);
					context.OutputMerger.SetDepthStencilState(depthStencilState);

					context.PixelShader.SetSampler(0, samplerState);

					ratio = (float)ClientSize.Width / (float)ClientSize.Height;

					projection = Matrix.PerspectiveFovRH(3.14F / 3.0F, ratio, 0.01f, 1000);
					view = Matrix.Invert(head);
					world = Matrix.Translation(0, 0, -100.0f);

					worldViewProjection = world * view * projection;

					//context.UpdateSubresource(ref worldViewProjection, worldViewProjectionBuffer);

					context.VertexShader.SetConstantBuffer(0, shaderParameterBuffer);
					context.PixelShader.SetConstantBuffer(0, shaderParameterBuffer);

					//Shapes.Cube.Begin(context);
					//Shapes.Cube.Draw(context);

					//Shapes.Sphere.Begin(context);
					//Shapes.Sphere.Draw(context);

					DrawPixels(worldViewProjection);

					// Draw Controllers
					context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

					Shaders.NormalTexture.Apply(context);

					context.PixelShader.SetSampler(0, samplerState);

					foreach (var controller in controllers)
					{
						context.InputAssembler.SetVertexBuffers(0, controllerVertexBufferBindings[controller]);
						context.InputAssembler.SetIndexBuffer(controllerIndexBuffers[controller], Format.R16_UInt, 0);

						Convert(ref currentPoses[controller].mDeviceToAbsoluteTracking, ref world);

						shaderParameters.WorldViewProjection = world * view * projection;
						shaderParameters.Diffuse = new Vector4(1, 1, 1, 1);

						context.UpdateSubresource(ref shaderParameters, shaderParameterBuffer);

						context.VertexShader.SetConstantBuffer(0, shaderParameterBuffer);
						context.PixelShader.SetConstantBuffer(0, shaderParameterBuffer);

						context.DrawIndexed((int)controllerModels[controller].unTriangleCount * 3 * 4, 0, 0);
					}

					// Show Backbuffer
					swapChain.Present(0, PresentFlags.None);
				}
			}
        }

		private void DrawPixels(Matrix worldViewProjection)
		{
			Matrix matrix = Matrix.Identity;

			uint index = 0;

			Shapes.Cube.Begin(context);

			for (int y = 0; y < UI.GameHeight; y++)
			{
				for (int x = 0; x < UI.GameWidth; x++)
				{
					var pixel = _ui.rawBitmap[index++];

					if (pixel == 0)
						continue;

					matrix.M41 = x - 128;
					matrix.M42 = 120 - y;

					Matrix.Multiply(ref matrix, ref worldViewProjection, out shaderParameters.WorldViewProjection);

					shaderParameters.Diffuse = SharpDX.Color.FromBgra(pixel).ToVector4();
	
					context.UpdateSubresource(ref shaderParameters, shaderParameterBuffer);

					Shapes.Cube.Draw(context);
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
        {
            Draw();
            base.OnPaint(e);
        }

		private static void Convert(ref HmdMatrix34_t source, ref Matrix destination)
		{
			destination.M11 = source.m0;
			destination.M21 = source.m1;
			destination.M31 = source.m2;
			destination.M41 = source.m3;
			destination.M12 = source.m4;
			destination.M22 = source.m5;
			destination.M32 = source.m6;
			destination.M42 = source.m7;
			destination.M13 = source.m8;
			destination.M23 = source.m9;
			destination.M33 = source.m10;
			destination.M43 = source.m11;
			//destination.M14 = 0.0f;
			//destination.M24 = 0.0f;
			//destination.M34 = 0.0f;
			//destination.M44 = 1.0f;
		}

		private static Matrix Convert(HmdMatrix34_t source)
		{
			var destination = new Matrix();

			destination.M11 = source.m0;
			destination.M21 = source.m1;
			destination.M31 = source.m2;
			destination.M41 = source.m3;
			destination.M12 = source.m4;
			destination.M22 = source.m5;
			destination.M32 = source.m6;
			destination.M42 = source.m7;
			destination.M13 = source.m8;
			destination.M23 = source.m9;
			destination.M33 = source.m10;
			destination.M43 = source.m11;
			destination.M14 = 0.0f;
			destination.M24 = 0.0f;
			destination.M34 = 0.0f;
			destination.M44 = 1.0f;

			return destination;
		}

		private static Matrix Convert(HmdMatrix44_t source)
		{
			var destination = new Matrix();

			destination.M11 = source.m0;
			destination.M21 = source.m1;
			destination.M31 = source.m2;
			destination.M41 = source.m3;
			destination.M12 = source.m4;
			destination.M22 = source.m5;
			destination.M32 = source.m6;
			destination.M42 = source.m7;
			destination.M13 = source.m8;
			destination.M23 = source.m9;
			destination.M33 = source.m10;
			destination.M43 = source.m11;
			destination.M14 = source.m12;
			destination.M24 = source.m13;
			destination.M34 = source.m14;
			destination.M44 = source.m15;

			return destination;
		}
	}
}