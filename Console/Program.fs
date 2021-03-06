﻿open vector
open particle
open integrator
open primitives
open shader
open Rendering
open OpenTK
open OpenTK.Input
open OpenTK.Graphics.OpenGL
open System.Drawing
open AntTweakBar
open TweakBar
open TweakBarGui
open BlinnMaterialTweakBar
open TweakBarGuiViewModel
open System.Reactive.Linq
open System.Linq

let transferMesh (m:mesh) =
    let vbos = Array.zeroCreate<int> 2
    GL.GenBuffers(2, vbos)

    GL.BindBuffer(BufferTarget.ArrayBuffer, vbos.[0])
    GL.BufferData(BufferTarget.ArrayBuffer, (nativeint)m.verticesSize, m.vertices, BufferUsageHint.StaticDraw)

    GL.BindBuffer(BufferTarget.ElementArrayBuffer, vbos.[1])
    GL.BufferData(BufferTarget.ElementArrayBuffer, (nativeint)m.elementSize, m.indices, BufferUsageHint.StaticDraw)

    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0)
    GL.EnableVertexAttribArray(0)
    GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, sizeof<float32> * 6, sizeof<Vector3>)
    GL.EnableVertexAttribArray(1)

let transferMeshWithNormals (m:meshWithNormals) =
    let vbo = GL.GenBuffer()

    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo)
    GL.BufferData(BufferTarget.ArrayBuffer, (nativeint)(m.vertices.Length * sizeof<V3N3>), m.vertices, BufferUsageHint.StaticDraw)

    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof<float32> * 6, 0)
    GL.EnableVertexAttribArray(0)
    GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, sizeof<float32> * 6, sizeof<Vector3>)
    GL.EnableVertexAttribArray(1)

let drawCube m =
    GL.DrawElements(BeginMode.Triangles, m.indices.Length, DrawElementsType.UnsignedShort, 0)

let drawMesh (m:meshWithNormals) (primitiveType:PrimitiveType) =
    GL.DrawArrays(primitiveType, 0, m.vertices.Length)

let makeRenderJob particle mesh cameraMatrix =
    let p = particle.position
    let translation = Matrix4.CreateTranslation(float32 p.x, float32 p.y, float32 p.z)
    let (modelToProjection:Matrix4) = translation * cameraMatrix;
    let normalMatrix = new Matrix3(Matrix4.Transpose(modelToProjection.Inverted()))
    {
        Mesh = mesh
        IndividualContext = {
                            ModelMatrix = translation
                            NormalMatrix = normalMatrix
            }
        }

let clamp min max v =
    match v with
    | _ when v < min -> min
    | _ when v > max -> max
    | _ -> v

type ShaderProgram =
    | SimpleShaderProgram of SimpleShaderProgram.SimpleProgram
    | NormalDebugShaderProgram of NormalDebugShaderProgram.SimpleProgram
    | BlinnShaderProgram of BlinnShaderProgram.BlinnPhongProgram

let render program renderJob =
    match program with
    | SimpleShaderProgram p ->
        GL.UseProgram p.ProgramId
        p.ProjectionMatrixUniform.set renderJob.StaticContext.ProjectionMatrix
        p.ViewMatrix.set renderJob.StaticContext.ViewMatrix
        for j in renderJob.RenderJobs do
            p.ModelMatrix.set j.IndividualContext.ModelMatrix
            p.NormalMatrix.set j.IndividualContext.NormalMatrix
            drawMesh j.Mesh PrimitiveType.Triangles
    | BlinnShaderProgram p ->
        GL.UseProgram p.ProgramId
        p.ProjectionMatrixUniform.set renderJob.StaticContext.ProjectionMatrix
        p.ViewMatrix.set renderJob.StaticContext.ViewMatrix
        match renderJob.Material with
        | Blinn m -> 
            p.AmbientColor.set m.AmbientColor
            p.DiffuseColor.set m.DiffuseColor
            p.SpecularColor.set m.SpecularColor
            ()
        | _ -> ()
        for j in renderJob.RenderJobs do
            p.ModelMatrix.set j.IndividualContext.ModelMatrix
            p.NormalMatrix.set j.IndividualContext.NormalMatrix
            drawMesh j.Mesh PrimitiveType.Triangles
    | NormalDebugShaderProgram p ->
        GL.UseProgram p.ProgramId
        p.ProjectionMatrixUniform.set renderJob.StaticContext.ProjectionMatrix
        p.ViewMatrix.set renderJob.StaticContext.ViewMatrix
        for j in renderJob.RenderJobs do
            p.ModelMatrix.set j.IndividualContext.ModelMatrix
            p.NormalMatrix.set j.IndividualContext.NormalMatrix
            drawMesh j.Mesh PrimitiveType.Points

let configureTweakBar c defaultValue =
    let bar = new Bar(c)
    bar.Size <- new Size(300, bar.Size.Height)
    bar.Label <- "Stuff"
    bar.Contained <- true
    makeViewModel bar defaultValue
        
let fsaaSamples = 8
let windowGraphicsMode =
    new Graphics.GraphicsMode(
            Graphics.GraphicsMode.Default.ColorFormat, 
            Graphics.GraphicsMode.Default.Depth, 
            Graphics.GraphicsMode.Default.Stencil,
            fsaaSamples)
type FysicsWindow() = 
    inherit GameWindow(800, 600, windowGraphicsMode) 

    let mutable particles = List.empty<particle>
    [<DefaultValue>] val mutable tweakbarContext : Context
    [<DefaultValue>] val mutable program : ShaderProgram
    [<DefaultValue>] val mutable program2 : ShaderProgram
    [<DefaultValue>] val mutable blinn : System.IObservable<BlinnMaterial>
    let defaultBlinnMaterial = { 
        AmbientColor = new Vector3(0.1f, 0.1f, 0.1f); 
        DiffuseColor = new Vector3(0.4f, 0.7f, 0.4f); 
        SpecularColor = new Vector3(1.0f, 1.0f, 1.0f); 
        SpecularExp = 150.0 }
    let defaultVm = { IntegrationSpeed = 1.0; BlinnMaterial = defaultBlinnMaterial }

    let mutable vm : ViewModel = defaultVm
    let mutable cameraPosition : Vector3 = new Vector3(0.0f, 0.0f, 5.0f)
    let particleTemplate = {
            position = { x = 0.0; y = 4.0; z = 0.0 }
            velocity = { x = 0.0; y = 0.0; z = 0.0 }
            acceleration = { x = 0.0; y = -9.8; z = 0.0 }
            damping = 0.999
            forceAccumulator = { x = 0.0; y = 0.0; z = 0.0 }
        }

    override this.OnLoad(e) =
        transferMeshWithNormals unitCubeWithNormals
        this.program <- BlinnShaderProgram BlinnShaderProgram.makeBlinnShaderProgram
//        this.program <- SimpleShaderProgram SimpleShaderProgram.makeSimpleShaderProgram
        this.program2 <- NormalDebugShaderProgram NormalDebugShaderProgram.makeSimpleShaderProgram
        this.tweakbarContext <- new Context(Tw.GraphicsAPI.OpenGL)
        let vmObs = configureTweakBar this.tweakbarContext defaultVm
        vmObs.Subscribe(fun m -> vm <- m) |> ignore
        GL.LineWidth(1.0f)
        GL.ClearColor(Color.WhiteSmoke)
        GL.Enable(EnableCap.DepthTest)
        this.VSync <- VSyncMode.On

    override this.OnClosing(e) =
        this.tweakbarContext.Dispose()

    override this.OnUpdateFrame(e) =
        if this.Keyboard.[Key.Escape] then do
            this.Exit()
        if this.Keyboard.[Key.A] then do
            cameraPosition <- Vector3.Transform(cameraPosition, Matrix4.CreateRotationY(float32 -e.Time))
        if this.Keyboard.[Key.D] then do
            cameraPosition <- Vector3.Transform(cameraPosition, Matrix4.CreateRotationY(float32 e.Time))

        particles <- particles 
            |> List.filter (fun p -> p.position.y > -50.0)
            |> List.toSeq 
            |> integrateAll (e.Time * vm.IntegrationSpeed)
            |> Seq.toList

    override this.OnKeyUp(e) =
        match convertKeyEvent e with
        | Some keys -> 
            this.tweakbarContext.HandleKeyUp(keys.Key, keys.Modifiers) |> ignore
        | _ -> ()

        if(e.Key = Key.Space) then do
            particles <- particleTemplate :: particles

    override this.OnKeyDown(e) =
        match convertKeyEvent e with
        | Some keys -> 
            this.tweakbarContext.HandleKeyDown(keys.Key, keys.Modifiers) |> ignore
        | None -> ()

    override this.OnKeyPress(e) =
        this.tweakbarContext.HandleKeyPress(e.KeyChar) |> ignore

    override this.OnMouseMove(e) =
        this.tweakbarContext.HandleMouseMove(new Point(e.X, e.Y)) |> ignore

    override this.OnMouseDown(e) =
        this.tweakbarContext.HandleMouseClick(Tw.MouseAction.Pressed, Tw.MouseButton.Left) |> ignore

    override this.OnMouseUp(e) =
        this.tweakbarContext.HandleMouseClick(Tw.MouseAction.Released, Tw.MouseButton.Left) |> ignore

    override this.OnResize(e) =
        GL.Viewport(0, 0, this.Width, this.Height)
        this.tweakbarContext.HandleResize(this.ClientSize)

    override this.OnRenderFrame(e) =
        GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)
        let aspectRatio = (float)this.Width / (float)this.Height
        let projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(2.0f, float32 aspectRatio, 0.1f, 100.0f)
        let cameraMatrix = Matrix4.LookAt(cameraPosition, Vector3.Zero, Vector3.UnitY)

        let blinnMaterial = vm.BlinnMaterial

        let staticRenderContext = {
                ProjectionMatrix = projectionMatrix
                ViewMatrix = cameraMatrix
            }
        let renderJob = {
                StaticContext = staticRenderContext
                RenderJobs = particles |> List.map (fun p -> makeRenderJob p unitCubeWithNormals cameraMatrix)
                Material = Blinn({ Rendering.BlinnMaterial.AmbientColor = blinnMaterial.AmbientColor; DiffuseColor = blinnMaterial.DiffuseColor; SpecularColor = blinnMaterial.SpecularColor})
            }

        render this.program renderJob
        render this.program2 renderJob

        this.tweakbarContext.Draw()

        this.SwapBuffers();

[<EntryPoint>]
let main argv =
    let window = new FysicsWindow()
    window.VSync <- VSyncMode.On
    window.Run()
    0 // return an integer exit code
