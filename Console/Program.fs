open vector
open OpenTK
open OpenTK.Input
open OpenTK.Graphics.OpenGL
open System.Drawing

type FysicsWindow() = 
    inherit GameWindow()

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
        GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0)

        GL.Begin(PrimitiveType.Triangles)

        GL.Color3(Color.MidnightBlue)
        GL.Vertex2(-1.0f, 1.0f)
        GL.Color3(Color.SpringGreen)
        GL.Vertex2(0.0f, -1.0f)
        GL.Color3(Color.Ivory)
        GL.Vertex2(1.0f, 1.0f)

        GL.End();

        this.SwapBuffers();

[<EntryPoint>]
let main argv =
    let window = new FysicsWindow()
    window.Run(60.0)
    0 // return an integer exit code
