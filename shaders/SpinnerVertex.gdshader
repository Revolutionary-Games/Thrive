shader_type canvas_item;

/*
 * Manually spins a texture via the updating of uniform "rotation".
 * UV rotation is done in vertex() so the math runs only at the quad's
 * corners and gets interpolated across fragments.
 */

/* Range: [-TAU, TAU]. Clockwise as it increases, counter-clockwise as it decreases. */
uniform float rotation = 0.0;

/*
 * Only override this for non-square spinners. UV space is normalized to
 * [0, 1] on both axes, so rotating a non-square texture in UV space makes
 * it wobble. Set this from the parent to the rect/tex (width, height) to fix
 * that.
 */
uniform vec2 aspect = vec2(1.0);

void vertex() {
    float angle = -rotation;
    mat2 rotationMatrix = mat2(vec2(cos(angle), -sin(angle)),
        vec2(sin(angle), cos(angle)));

    vec2 uv = (UV - vec2(0.5)) * aspect;
    uv = uv * rotationMatrix;
    UV = uv / aspect + vec2(0.5);
}
