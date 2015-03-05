module SimpleShaderProgram
open OpenTK.Graphics.OpenGL
open shader

let vertexShaderSource =
    "#version 400 

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;

out vec3 vNormal;

void main()
{
    gl_Position = projectionMatrix * viewMatrix * modelMatrix * vec4(position, 1.0);
    vNormal = normal;
}"

let fragmentShaderSource = 
    "#version 400 

uniform mat3 normalMatrix;
uniform mat4 viewMatrix;

out vec4 outColor;
in vec3 vNormal;

vec3 lightDir = normalize(vec3(-1.0, -0.2, 0.0));

void main()
{
    vec3 normal = normalize(normalMatrix * vNormal);
    vec3 dirToLight = -normalize((viewMatrix * vec4(lightDir, 0)).xyz);
    float incidence = clamp(0.0, 1.0, dot(normal, dirToLight));
    outColor = (incidence * vec4(1.0)) + vec4(0.2, 0.2, 0.2, 1.0);
}"

let rawShaders = [ 
    (ShaderType.VertexShader, vertexShaderSource); 
    (ShaderType.FragmentShader, fragmentShaderSource) ]

type SimpleProgram = {
        ProgramId : int
        ProjectionMatrixUniform : MatrixUniform
        ViewMatrix : MatrixUniform
        ModelMatrix : MatrixUniform
        NormalMatrix : Matrix3Uniform
    }

let makeSimpleShaderProgram =
    match makeProgram rawShaders with
    | Some programId -> 
        { 
            ProgramId = programId; 
            ProjectionMatrixUniform = makeMatrixUniform programId "projectionMatrix"
            ViewMatrix = makeMatrixUniform programId "viewMatrix"
            ModelMatrix = makeMatrixUniform programId "modelMatrix"
            NormalMatrix = makeMatrix3Uniform programId "normalMatrix"
        }
    | _ -> failwith "Program compilation failed"

