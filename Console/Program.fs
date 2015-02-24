open vector
open particle
open integrator
open primitives
open shader
open OpenTK
open OpenTK.Input
open OpenTK.Graphics.OpenGL
open System.Drawing

let transferMesh m =
    let mutable vbo = -1
    GL.GenBuffers(1, &vbo)
    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo)
    let vertices = m.vertices
    GL.BufferData(BufferTarget.ArrayBuffer, (nativeint)m.verticesSize, vertices, BufferUsageHint.StaticDraw)

let vertexShaderSource =
    "#version 150

in vec3 position;

void main()
{
    gl_Position = vec4(position, 1.0);
}"

let fragmentShaderSource = 
    "#version 150

out vec4 outColor;

void main()
{
    outColor = vec4(1.0, 1.0, 1.0, 1.0);
}"


let rawShaders = [ 
    (ShaderType.VertexShader, vertexShaderSource); 
    (ShaderType.FragmentShader, fragmentShaderSource) ]

let drawCube (pos : Vector3d) (color : Color) =
    GL.Color3(color)
    for i in unitCube.indices do
        GL.Vertex3(unitCube.vertices.[i] + pos)
        
type FysicsWindow() = 
    inherit GameWindow()

    let mutable elapsedTime = 0.0
    let mutable particles = List.empty<particle>
    let particleTemplate = {
            position = { x = 0.0; y = 4.0; z = 0.0 }
            velocity = { x = 0.0; y = 0.0; z = 0.0 }
            acceleration = { x = 0.0; y = -9.8; z = 0.0 }
            damping = 0.999
        }

    override this.OnLoad(e) =
        let programResult = makeProgram rawShaders
        this.VSync <- VSyncMode.On

    override this.OnUpdateFrame(e) =
        if this.Keyboard.[Key.Space] then do
            particles <- particleTemplate :: particles
        particles <- particles |> List.filter (fun p -> p.position.y > -50.0)
        particles <- Seq.toList (integrateAll e.Time (List.toSeq particles))
        let a = this.Keyboard.[Key.Escape]
        if a then do
            this.Exit()

    override this.OnResize(e) =
        GL.Viewport(0, 0, this.Width, this.Height)

    override this.OnRenderFrame(e) =
        GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)

        GL.MatrixMode(MatrixMode.Projection)
        let aspectRatio = (float)this.Width / (float)this.Height
        let projection = Matrix4d.CreatePerspectiveFieldOfView(2.0, aspectRatio, 0.1, 100.0)
        GL.LoadMatrix(ref projection)

        GL.MatrixMode(MatrixMode.Modelview)
        let modelView = Matrix4d.LookAt(new Vector3d(0.0, 1.0, 5.0), Vector3d.Zero, Vector3d.UnitY)
        GL.LoadMatrix(ref modelView)
        GL.CullFace(CullFaceMode.Back)
        GL.Enable(EnableCap.CullFace)
        GL.Enable(EnableCap.DepthTest)
        GL.FrontFace(FrontFaceDirection.Ccw)

        GL.PointSize(10.f)
        GL.Begin(BeginMode.Triangles)

        for particle in particles do
            let p = particle.position
            drawCube (new Vector3d(p.x, p.y, p.z)) Color.MidnightBlue

        GL.End();

        this.SwapBuffers();

        elapsedTime <- elapsedTime + e.Time

[<EntryPoint>]
let main argv =
    let window = new FysicsWindow()
    window.Run(60.0)
    0 // return an integer exit code
