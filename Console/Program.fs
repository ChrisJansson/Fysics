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
        indices = [| |] 
    }
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
        GL.LoadIdentity()
        GL.Ortho(-1.0, 1.0, -1.0, 1.0, -1.0, 4.0)
        GL.Rotate(elapsedTime, Vector3d.UnitY)

        GL.PointSize(10.f)
        GL.Begin(BeginMode.Points)

        GL.Color3(Color.MidnightBlue)
        for v in unitCube.vertices do
            GL.Vertex3(v)

        GL.End();

        this.SwapBuffers();

        elapsedTime <- elapsedTime + e.Time

[<EntryPoint>]
let main argv =
    let window = new FysicsWindow()
    window.Run(60.0)
    0 // return an integer exit code
