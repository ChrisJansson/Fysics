open vector
open OpenTK
open OpenTK.Input
open OpenTK.Graphics.OpenGL
open System.Drawing

type cube = { 
        vertices : Vector3d []
        indices : int []
    }

let unitCube = { 
        vertices = 
            [| 
                new Vector3d(-0.5, -0.5, -0.5) //back
                new Vector3d(0.5, -0.5, -0.5)
                new Vector3d(0.5, 0.5, -0.5)
                new Vector3d(-0.5, 0.5, -0.5)
                new Vector3d(-0.5, -0.5, 0.5) //front
                new Vector3d(0.5, -0.5, 0.5)
                new Vector3d(0.5, 0.5, 0.5)
                new Vector3d(-0.5, 0.5, 0.5)
            |]
        indices = 
            [| 
                4; 6; 7; //front
                4; 5; 6;
                6; 5; 1; //right
                6; 1; 2;
                0; 4; 7; //left
                0; 7; 3;
                0; 2; 1; //back
                0; 3; 2;
                7; 6; 2; //top
                7; 2; 3;
                4; 5; 1; //bottom
                4; 1; 0;
            |] 
    }

let colors = [|
        Color.Azure
        Color.Beige
        Color.Brown
        Color.Crimson
        Color.HotPink
        Color.Honeydew
    |]

type FysicsWindow() = 
    inherit GameWindow()

    let mutable elapsedTime = 0.0

    override this.OnLoad(e) =
        this.VSync <- VSyncMode.On

    override this.OnUpdateFrame(e) =
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
        let modelView = Matrix4d.RotateY(elapsedTime) * Matrix4d.LookAt(new Vector3d(0.0, System.Math.Sin(elapsedTime), 2.0), Vector3d.Zero, Vector3d.UnitY)
        GL.LoadMatrix(ref modelView)
        GL.CullFace(CullFaceMode.Back)
        GL.Enable(EnableCap.CullFace)
        GL.Enable(EnableCap.DepthTest)
        GL.FrontFace(FrontFaceDirection.Ccw)

        GL.PointSize(10.f)
        GL.Begin(BeginMode.Triangles)

        GL.Color3(Color.MidnightBlue)
        for v in unitCube.indices do
            GL.Color3(colors.[v % colors.Length])
            GL.Vertex3(unitCube.vertices.[v])

        GL.End();

        this.SwapBuffers();

        elapsedTime <- elapsedTime + e.Time

[<EntryPoint>]
let main argv =
    let window = new FysicsWindow()
    window.Run(60.0)
    0 // return an integer exit code
