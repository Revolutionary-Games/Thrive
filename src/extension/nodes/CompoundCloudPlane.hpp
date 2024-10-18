#pragma once

#include <godot_cpp/classes/mesh_instance3d.hpp>
#include <godot_cpp/classes/shader_material.hpp>
#include <godot_cpp/classes/image_texture.hpp>
#include <godot_cpp/classes/noise_texture2d.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/packed_int32_array.hpp>
#include <mutex>
#include <vector>
#include <unordered_map>

namespace Thrive {

class CompoundCloudPlane final : public godot::MeshInstance3D {
    GDCLASS(CompoundCloudPlane, godot::MeshInstance3D)

protected:
    static void _bind_methods();

public:
    CompoundCloudPlane();
    ~CompoundCloudPlane();

    void _ready() override;
    void _process(double delta) override;

    int64_t get_native_instance();
    void init(int render_priority, int cloud1, int cloud2, int cloud3, int cloud4);
    void update_position(const godot::Vector2& new_position);
    void update_texture();
    void set_brightness(float brightness);
    void add_cloud(int compound, int x, int y, float density);
    bool contains_position(const godot::Vector3& world_position) const;
    godot::Vector2 convert_to_cloud_local(const godot::Vector3& world_position) const;
    void clear_contents();
    float take_compound(int compound, int x, int y, float fraction);
    float amount_available(int compound, int x, int y, float fraction) const;
    bool contains_position_with_radius(const godot::Vector3& world_position, float radius) const;
    godot::Vector3 convert_to_world(int cloud_x, int cloud_y) const;

    godot::PackedInt32Array get_compounds() const;
    void set_compounds(const godot::PackedInt32Array& compounds);
    int get_resolution() const;
    void set_resolution(int resolution);
    int get_size() const;
    void set_size(int size);

    godot::PackedInt32Array get_compounds_array() const;
    int get_compounds_count() const;

private:
    void create_mesh();
    void create_shader_material();
    void create_noise_texture();
    void update_cloud_texture();
    void update_shader_parameters();
    void diffuse(float delta);
    void advect(float delta);
    bool has_compounds() const;
    void update_compound_colors();
    int get_dominant_compound_at(int x, int y) const;
    float get_total_density_at(int x, int y) const;
    godot::Color get_color_at(int x, int y) const;

    godot::Ref<godot::ShaderMaterial> material;
    godot::Ref<godot::ImageTexture> densities_texture;
    godot::Ref<godot::NoiseTexture2D> noise_texture;

    godot::PackedInt32Array compounds;
    int resolution;
    int size;
    godot::Vector2 position;
    godot::Vector3 cached_world_position;
    float brightness;

    std::vector<std::vector<godot::Vector4>> density;
    std::vector<std::vector<godot::Vector4>> old_density;
    mutable std::mutex density_mutex;

    static const int CLOUDS_IN_ONE = 4;
    float decay_rates[CLOUDS_IN_ONE];
    godot::Color compound_colors[CLOUDS_IN_ONE];

    static std::unordered_map<int64_t, CompoundCloudPlane*> instances;
    static int64_t next_instance_id;
    int64_t instance_id;

    bool texture_needs_update;
    float minimum_visible_density;
};

} // namespace Thrive