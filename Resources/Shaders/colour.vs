#version 130

uniform mat4 mvp;

in vec2 in_position;
in vec4 in_color;
out vec4 vs_color;

void main(void)
{
	gl_Position = mvp * vec4(in_position.xy, 0., 1.0); 
	vs_color = in_color;
}