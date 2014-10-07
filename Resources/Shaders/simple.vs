#version 430 core

uniform mat4 mvp;

in vec2 position;
out vec4 vs_color;

void main(void)
{
	const vec4 vertices[3] = vec4[3](vec4( 0.25, -0.25, 0.5, 1.0),
									vec4(-0.25, -0.25, 0.5, 1.0),
									vec4( 0.25, 0.25, 0.5, 1.0));
	if (mvp != mat4(0))
		gl_Position = mvp * vec4(position.xy, 0., 1.0); 
	//else
		//gl_Position = mvp * vertices[int(mod(gl_VertexID,3))];
	vs_color = vec4(1.0, 1.0, 1.0, 1.0);
}