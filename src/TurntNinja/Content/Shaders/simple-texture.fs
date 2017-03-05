
uniform sampler2D tex_object;
uniform float alpha;

in vec2 tc;

out vec4 fragColour;

void main(void)
{
	fragColour = texture(tex_object, tc) * vec4(1., 1., 1., alpha);
}