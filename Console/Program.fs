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

let drawCubeWithNormals (m:meshWithNormals) (primitiveType:PrimitiveType) =
    GL.DrawArrays(primitiveType, 0, m.vertices.Length)

let makeRenderJob particle mesh cameraMatrix =
       
    let p = particle.position
    let translation = Matrix4.CreateTranslation(float32 p.x, float32 p.y, float32 p.z)
    let (modelToProjection:Matrix4) = translation * cameraMatrix;
    let normalMatrix = new Matrix3(Matrix4.Transpose(modelToProjection.Inverted()))
    let renderJob = {
            Mesh = mesh
            IndividualContext = {
                                ModelMatrix = translation
                                NormalMatrix = normalMatrix
            }
        }
    renderJob
        
type FysicsWindow() = 
    inherit GameWindow()

    let mutable elapsedTime = 0.0
    let mutable particles = List.empty<particle>
    [<DefaultValue>] val mutable program : SimpleShaderProgram.SimpleProgram
    [<DefaultValue>] val mutable program2 : NormalDebugShaderProgram.SimpleProgram
    let particleTemplate = {
            position = { x = 0.0; y = 4.0; z = 0.0 }
            velocity = { x = 0.0; y = 0.0; z = 0.0 }
            acceleration = { x = 0.0; y = -9.8; z = 0.0 }
            damping = 0.999
        }

    override this.OnLoad(e) =
        transferMeshWithNormals unitCubeWithNormals
        this.program2 <- NormalDebugShaderProgram.makeSimpleShaderProgram
        this.program <- SimpleShaderProgram.makeSimpleShaderProgram
        GL.UseProgram this.program2.ProgramId
        GL.LineWidth(1.0f)
        this.VSync <- VSyncMode.On

    override this.OnUpdateFrame(e) =
        if this.Keyboard.[Key.Space] then do
            particles <- particleTemplate :: particles
        let a = this.Keyboard.[Key.Escape]
        if a then do
            this.Exit()

        particles <- particles |> List.filter (fun p -> p.position.y > -50.0)
        particles <- Seq.toList (integrateAll e.Time (List.toSeq particles))

    override this.OnResize(e) =
        GL.Viewport(0, 0, this.Width, this.Height)

    override this.OnRenderFrame(e) =
        GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)
        let aspectRatio = (float)this.Width / (float)this.Height
        let projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(2.0f, float32 aspectRatio, 0.1f, 100.0f)
        let cameraMatrix = Matrix4.LookAt(new Vector3(5.0f, 1.0f, 5.0f), Vector3.Zero, Vector3.UnitY)

        let staticRenderContext = {
                ProjectionMatrix = projectionMatrix
                ViewMatrix = cameraMatrix
            }
        let renderJob = {
                StaticContext = staticRenderContext
                RenderJobs = particles |> List.map (fun p -> makeRenderJob p unitCubeWithNormals cameraMatrix)ndividualRenderJobs
            }

        GL.UseProgram this.program.ProgramId
        this.program.ProjectionMatrixUniform.set renderJob.StaticContext.ProjectionMatrix
        this.program.ViewMatrix.set renderJob.StaticContext.ViewMatrix
        for j in renderJob.RenderJobs do
            this.program.ModelMatrix.set j.IndividualContext.ModelMatrix
            drawCubeWithNormals j.Mesh PrimitiveType.Triangles

        GL.UseProgram this.program2.ProgramId
        this.program2.ProjectionMatrixUniform.set renderJob.StaticContext.ProjectionMatrix
        this.program2.ViewMatrix.set renderJob.StaticContext.ViewMatrix
        for j in renderJob.RenderJobs do
            this.program2.ModelMatrix.set j.IndividualContext.ModelMatrix
            this.program2.NormalMatrix.set j.IndividualContext.NormalMatrix
            drawCubeWithNormals j.Mesh PrimitiveType.Points

        this.SwapBuffers();
        elapsedTime <- elapsedTime + e.Time

[<EntryPoint>]
let main argv =
    let window = new FysicsWindow()
    window.VSync <- VSyncMode.On
    window.Run()
    0 // return an integer exit code
