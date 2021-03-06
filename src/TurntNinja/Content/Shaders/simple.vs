
uniform mat4 mvp;
uniform vec4 in_color;

in vec2 in_position;
out vec4 vs_color;

void main(void)
{
	gl_Position = mvp * vec4(in_position.xy, 0., 1.0); 
	vs_color = in_color;
}
