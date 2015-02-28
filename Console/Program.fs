open vector
open particle
open integrator
open primitives
open shader
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

let drawCube m =
    GL.DrawElements(BeginMode.Triangles, m.indices.Length, DrawElementsType.UnsignedShort, 0)
        
type FysicsWindow() = 
    inherit GameWindow()

    let mutable elapsedTime = 0.0
    let mutable particles = List.empty<particle>
    [<DefaultValue>] val mutable program : SimpleShaderProgram.SimpleProgram
    let particleTemplate = {
            position = { x = 0.0; y = 4.0; z = 0.0 }
            velocity = { x = 0.0; y = 0.0; z = 0.0 }
            acceleration = { x = 0.0; y = -9.8; z = 0.0 }
            damping = 0.999
        }

    override this.OnLoad(e) =
        transferMesh unitCube
        this.program <- SimpleShaderProgram.makeSimpleShaderProgram
        GL.UseProgram this.program.ProgramId
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0)
        GL.EnableVertexAttribArray(0)
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
        let cameraMatrix = Matrix4.LookAt(new Vector3(0.0f, 1.0f, 5.0f), Vector3.Zero, Vector3.UnitY)
        this.program.ProjectionMatrixUniform.set projectionMatrix
        this.program.ViewMatrix.set cameraMatrix

        for particle in particles do
            let p = particle.position
            let translation = Matrix4.CreateTranslation(float32 p.x, float32 p.y, float32 p.z)
            this.program.ModelMatrix.set translation
            drawCube unitCube

        this.SwapBuffers();
        elapsedTime <- elapsedTime + e.Time

[<EntryPoint>]
let main argv =
    let window = new FysicsWindow()
    window.VSync <- VSyncMode.On
    window.Run()
    0 // return an integer exit code
