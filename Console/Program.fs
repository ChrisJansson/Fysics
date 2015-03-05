open vector
open particle
open integrator
open primitives
open shader
open Rendering
open OpenTK
open OpenTK.Input
open OpenTK.Graphics.OpenGL
open System.Drawing

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
    | NormalDebugShaderProgram p ->
        GL.UseProgram p.ProgramId
        p.ProjectionMatrixUniform.set renderJob.StaticContext.ProjectionMatrix
        p.ViewMatrix.set renderJob.StaticContext.ViewMatrix
        for j in renderJob.RenderJobs do
            p.ModelMatrix.set j.IndividualContext.ModelMatrix
            p.NormalMatrix.set j.IndividualContext.NormalMatrix
            drawMesh j.Mesh PrimitiveType.Points
    
        
type FysicsWindow() = 
    inherit GameWindow()

    let mutable integrationSpeed = 1.0
    let mutable particles = List.empty<particle>
    let clampIntegrationSpeed = clamp 0.01 2.0
    [<DefaultValue>] val mutable program : ShaderProgram
    [<DefaultValue>] val mutable program2 : ShaderProgram
    let mutable cameraPosition : Vector3 = new Vector3(0.0f, 0.0f, 5.0f)
    let particleTemplate = {
            position = { x = 0.0; y = 4.0; z = 0.0 }
            velocity = { x = 0.0; y = 0.0; z = 0.0 }
            acceleration = { x = 0.0; y = -9.8; z = 0.0 }
            damping = 0.999
        }

    override this.OnLoad(e) =
        transferMeshWithNormals unitCubeWithNormals
        this.program <- SimpleShaderProgram SimpleShaderProgram.makeSimpleShaderProgram
        this.program2 <- NormalDebugShaderProgram NormalDebugShaderProgram.makeSimpleShaderProgram
        GL.LineWidth(1.0f)
        GL.ClearColor(Color.WhiteSmoke)
        GL.Enable(EnableCap.DepthTest)
        this.VSync <- VSyncMode.On

    override this.OnUpdateFrame(e) =
        if this.Keyboard.[Key.Escape] then do
            this.Exit()
        if this.Keyboard.[Key.P] then do
            integrationSpeed <- clampIntegrationSpeed integrationSpeed + 0.01
        if this.Keyboard.[Key.O] then do
            integrationSpeed <- 1.0
        if this.Keyboard.[Key.I] then do
            integrationSpeed <- clampIntegrationSpeed integrationSpeed - 0.01
        if this.Keyboard.[Key.A] then do
            cameraPosition <- Vector3.Transform(cameraPosition, Matrix4.CreateRotationY(float32 -e.Time))
        if this.Keyboard.[Key.D] then do
            cameraPosition <- Vector3.Transform(cameraPosition, Matrix4.CreateRotationY(float32 e.Time))

        particles <- particles 
            |> List.filter (fun p -> p.position.y > -50.0)
            |> List.toSeq 
            |> integrateAll (e.Time * integrationSpeed)
            |> Seq.toList

    override this.OnKeyUp(e) =
        if(e.Key = Key.Space) then do
            particles <- particleTemplate :: particles

    override this.OnResize(e) =
        GL.Viewport(0, 0, this.Width, this.Height)

    override this.OnRenderFrame(e) =
        GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)
        let aspectRatio = (float)this.Width / (float)this.Height
        let projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(2.0f, float32 aspectRatio, 0.1f, 100.0f)
        let cameraMatrix = Matrix4.LookAt(cameraPosition, Vector3.Zero, Vector3.UnitY)

        let staticRenderContext = {
                ProjectionMatrix = projectionMatrix
                ViewMatrix = cameraMatrix
            }
        let renderJob = {
                StaticContext = staticRenderContext
                RenderJobs = particles |> List.map (fun p -> makeRenderJob p unitCubeWithNormals cameraMatrix)
            }

        render this.program renderJob
        render this.program2 renderJob

        this.SwapBuffers();

[<EntryPoint>]
let main argv =
    let window = new FysicsWindow()
    window.VSync <- VSyncMode.On
    window.Run()
    0 // return an integer exit code
