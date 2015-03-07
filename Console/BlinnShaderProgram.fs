module BlinnShaderProgram
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
out vec3 vPosition;

void main()
{
    gl_Position = projectionMatrix * viewMatrix * modelMatrix * vec4(position, 1.0);
    vPosition = position;
    vNormal = normal;
}"

let fragmentShaderSource = 
    "#version 400 

uniform mat3 normalMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;

out vec4 outColor;
in vec3 vNormal;
in vec3 vPosition;

const vec3 ambientColor = vec3(0.0, 0.1, 0.0);
const vec3 diffuseColor = vec3(0.7, 1.0, 0.7);
const vec3 specColor = vec3(1.0, 1.0, 1.0);
const vec4 lightPosition = vec4(10.0, 5.0, 0.0, 1.0);

vec3 calculateDiffuse(vec3 diffuseColor, vec3 dirToLight, vec3 normal) {
    float incidence = dot(dirToLight, normal);
    return max(0.0, incidence) * diffuseColor; 
}

vec3 calculateSpecular(vec3 dirToLight, vec3 dirToEye, vec3 normal) {
    vec3 h = normalize(dirToLight + dirToEye);
    float specularAngle = max(0.0, dot(h, normal));
    return pow(specularAngle, 100.0);
}

void main()
{
    vec3 normal = normalize(normalMatrix * vNormal);
    vec4 position = viewMatrix * modelMatrix * vec4(vPosition, 1.0);

    vec3 dirToLight = normalize((viewMatrix * lightPosition - position).xyz);
    vec3 diffuse = calculateDiffuse(diffuseColor, dirToLight, normal);

    vec3 dirToEye = normalize(-(position.xyz));
    vec3 specular = calculateSpecular(dirToLight, dirToEye, normal);

    outColor = vec4(ambientColor + diffuse + specular, 1.0);
}"

let rawShaders = [ 
    (ShaderType.VertexShader, vertexShaderSource); 
    (ShaderType.FragmentShader, fragmentShaderSource) ]

type BlinnPhongProgram = {
        ProgramId : int
        ProjectionMatrixUniform : MatrixUniform
        ViewMatrix : MatrixUniform
        ModelMatrix : MatrixUniform
        NormalMatrix : Matrix3Uniform
    }

let makeBlinnShaderProgram =
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

