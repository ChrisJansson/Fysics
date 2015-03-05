module primitives
open OpenTK

type mesh = { 
        vertices : Vector3 []
        indices : uint16 []
    }
    with
    member this.verticesSize = this.vertices.Length * sizeof<Vector3>
    member this.elementSize = this.indices.Length * sizeof<uint16>

let unitCube = { 
        vertices = 
            [| 
                new Vector3(-0.5f, -0.5f, -0.5f) //back
                new Vector3(0.5f, -0.5f, -0.5f)
                new Vector3(0.5f, 0.5f, -0.5f)
                new Vector3(-0.5f, 0.5f, -0.5f)
                new Vector3(-0.5f, -0.5f, 0.5f) //front
                new Vector3(0.5f, -0.5f, 0.5f)
                new Vector3(0.5f, 0.5f, 0.5f)
                new Vector3(-0.5f, 0.5f, 0.5f)
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
                4; 1; 5; //bottom
                4; 0; 1;
            |] |> Array.map (fun i -> uint16 i)
    }

type V3N3 = 
    struct
        val Vertex : Vector3
        val Normal : Vector3
        new(vertex : Vector3, normal : Vector3) = { Vertex = vertex; Normal = normal }
    end

type meshWithNormals = {
        vertices : V3N3 []
    }

let unitPlane =
    let vertices = [ 
        new Vector3(-0.5f, 0.0f, 0.5f)
        new Vector3(0.5f, 0.0f, 0.5f)
        new Vector3(0.5f, 0.0f, -0.5f)
        new Vector3(-0.5f, 0.0f, -0.5f)]

    {
        meshWithNormals.vertices = vertices |> List.map (fun v -> new V3N3(v, Vector3.UnitY)) |> List.toArray
    }

let unitCubeWithNormals =
    let vertices = unitCube.indices |> Array.map (fun i -> unitCube.vertices.[int i])
    let normals = Array.zeroCreate<Vector3> vertices.Length
    for i = 0 to (vertices.Length / 3) - 1 do
        let v0 = vertices.[i * 3]
        let v1 = vertices.[i * 3 + 1]
        let v2 = vertices.[i * 3 + 2]
        let first = v1 - v0
        let second = v2 - v0
        let normal = Vector3.Cross(first, second).Normalized()
        normals.[i * 3] <-  normal
        normals.[i * 3 + 1] <-  normal
        normals.[i * 3 + 2] <-  normal
    { vertices = Array.zip vertices normals |> Array.map (fun (v, n) -> new V3N3(v, n))}
        
