shader_type spatial;

// NOTE: not used yet as SpatialMaterial can handle everything we need currently
// Look in MulticellularMetaballDisplayer

void fragment() {
    ALBEDO = COLOR.rgb;
}
