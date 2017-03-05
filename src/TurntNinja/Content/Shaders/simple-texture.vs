
uniform mat4 mvp;

in vec2 in_position;
in vec2 in_tc;

out vec2 tc;

void main(void)
{
	tc = in_tc;
	gl_Position = mvp * vec4(in_position.xy, 0., 1.0); 
}
