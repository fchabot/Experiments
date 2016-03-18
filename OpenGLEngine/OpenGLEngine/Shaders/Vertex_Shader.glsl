#version 400 core //OpenGL version

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec4 in_color;

out vec4 color;

vec4 translate(vec4 aInput, vec3 aTranslation)
{
	mat4 mat = mat4(1.0, 0.0, 0.0, aTranslation.x,
					0.0, 1.0, 0.0, aTranslation.y,
					0.0, 0.0, 1.0, aTranslation.z,
					0.0, 0.0, 0.0, 1.0);

	return mat * aInput;
}
 
void main()
{
	color = in_color;

	vec4 finalPosition = vec4(in_position, 1.0);
	finalPosition = translate(finalPosition, vec3(10.0, 0.0, 0.0));

	gl_Position = finalPosition;
}