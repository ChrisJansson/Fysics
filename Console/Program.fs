open vector
open particle
open integrator
open primitives
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

type shaderCompilationStatus = 
    | Success of shaderId : int
    | Error of message : string

let getShaderCompilationError (shaderId:int) =
    GL.GetShaderInfoLog(shaderId)

let getShaderCompilationStatus (shaderId:int) =
    let mutable status = -1
    GL.GetShader(shaderId, ShaderParameter.CompileStatus, &status)
    let convertedStatus = enum status
    match convertedStatus with
        | Boolean.True -> Success shaderId
        | _ -> Error (getShaderCompilationError shaderId)

let makeShader shaderType source =
    let shaderId = GL.CreateShader shaderType
    GL.ShaderSource(shaderId, source)
    GL.CompileShader(shaderId)
    getShaderCompilationStatus shaderId

let makeProgram shaders =
    let programId = GL.CreateProgram()
    for shaderId in shaders do
        GL.AttachShader(programId, shaderId)
    GL.LinkProgram programId

let compiledSuccessfully shader =
    match shader with
    | Success shaderId -> true
    | Error message -> false

let rawShaders = [| 
    (ShaderType.VertexShader, vertexShaderSource); 
    (ShaderType.FragmentShader, fragmentShaderSource) |]

let rec extract compiledShaders =
    match compiledShaders with
    | [] -> Some []
    | head::tail -> 
        match head with
        | Success shaderId -> 
            match extract tail with
            | Some shaderIds -> Some (head::shaderIds)
            | None -> None
        | Error message -> None

let matchStuff s shader =
    let pair = (s, shader)
    match pair with
    | (Some x, Success shaderId) -> Some (shaderId::x)
    | _ -> None

let extract2 compiledShaders =
    compiledShaders |> Seq.fold (fun s shader -> matchStuff) List.empty compiledShaders

let stuff =
    let compiledShaders = rawShaders |> Seq.map (fun (st, ss) -> makeShader st ss)


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
