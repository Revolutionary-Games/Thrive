#include "CompoundCloudPlane.hpp"
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/classes/shader.hpp>
#include <godot_cpp/classes/quad_mesh.hpp>
#include <godot_cpp/classes/fast_noise_lite.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

namespace Thrive {

std::unordered_map<int64_t, CompoundCloudPlane*> CompoundCloudPlane::instances;
int64_t CompoundCloudPlane::next_instance_id = 1;

void CompoundCloudPlane::_bind_methods() {
    using namespace godot;

    ClassDB::bind_method(D_METHOD("get_native_instance"), &CompoundCloudPlane::get_native_instance);
    ClassDB::bind_method(D_METHOD("init", "render_priority", "cloud1", "cloud2", "cloud3", "cloud4"), &CompoundCloudPlane::init);
    ClassDB::bind_method(D_METHOD("update_position", "new_position"), &CompoundCloudPlane::update_position);
    ClassDB::bind_method(D_METHOD("update_texture"), &CompoundCloudPlane::update_texture);
    ClassDB::bind_method(D_METHOD("set_brightness", "brightness"), &CompoundCloudPlane::set_brightness);
    ClassDB::bind_method(D_METHOD("add_cloud", "compound", "x", "y", "density"), &CompoundCloudPlane::add_cloud);
    ClassDB::bind_method(D_METHOD("contains_position", "world_position"), &CompoundCloudPlane::contains_position);
    ClassDB::bind_method(D_METHOD("convert_to_cloud_local", "world_position"), &CompoundCloudPlane::convert_to_cloud_local);
    ClassDB::bind_method(D_METHOD("clear_contents"), &CompoundCloudPlane::clear_contents);
    ClassDB::bind_method(D_METHOD("take_compound", "compound", "x", "y", "fraction"), &CompoundCloudPlane::take_compound);
    ClassDB::bind_method(D_METHOD("amount_available", "compound", "x", "y", "fraction"), &CompoundCloudPlane::amount_available);
    ClassDB::bind_method(D_METHOD("contains_position_with_radius", "world_position", "radius"), &CompoundCloudPlane::contains_position_with_radius);
    ClassDB::bind_method(D_METHOD("convert_to_world", "cloud_x", "cloud_y"), &CompoundCloudPlane::convert_to_world);

    ClassDB::bind_method(D_METHOD("get_compounds"), &CompoundCloudPlane::get_compounds);
    ClassDB::bind_method(D_METHOD("set_compounds", "compounds"), &CompoundCloudPlane::set_compounds);
    ClassDB::bind_method(D_METHOD("get_resolution"), &CompoundCloudPlane::get_resolution);
    ClassDB::bind_method(D_METHOD("set_resolution", "resolution"), &CompoundCloudPlane::set_resolution);
    ClassDB::bind_method(D_METHOD("get_size"), &CompoundCloudPlane::get_size);
    ClassDB::bind_method(D_METHOD("set_size", "size"), &CompoundCloudPlane::set_size);
    ClassDB::bind_method(D_METHOD("get_compounds_array"), &CompoundCloudPlane::get_compounds_array);
    ClassDB::bind_method(D_METHOD("get_compounds_count"), &CompoundCloudPlane::get_compounds_count);

    ADD_PROPERTY(PropertyInfo(Variant::PACKED_INT32_ARRAY, "compounds"), "set_compounds", "get_compounds");
    ADD_PROPERTY(PropertyInfo(Variant::INT, "resolution"), "set_resolution", "get_resolution");
    ADD_PROPERTY(PropertyInfo(Variant::INT, "size"), "set_size", "get_size");
}

CompoundCloudPlane::CompoundCloudPlane() {
    instance_id = next_instance_id++;
    instances[instance_id] = this;
    resolution = 0;
    size = 0;
    brightness = 1.0f;
    position = godot::Vector2(0, 0);
    texture_needs_update = false;
    minimum_visible_density = 0.01f;

    for (int i = 0; i < CLOUDS_IN_ONE; ++i) {
        decay_rates[i] = 0.99f - (i * 0.01f);
        compound_colors[i] = godot::Color(1, 1, 1, 1);  
    }
}

CompoundCloudPlane::~CompoundCloudPlane() {
    instances.erase(instance_id);
}

void CompoundCloudPlane::_ready() {
    create_mesh();
    create_shader_material();
    create_noise_texture();
    if (size > 0) {
        update_cloud_texture();
    }
    update_shader_parameters();

    cached_world_position = get_global_transform().origin;
    set_process(false);  // Start with processing disabled
}

void CompoundCloudPlane::_process(double delta) {
    diffuse(static_cast<float>(delta));
    advect(static_cast<float>(delta));
    
    if (texture_needs_update) {
        update_cloud_texture();
        texture_needs_update = false;
    }
    
    update_shader_parameters();

    if (!has_compounds()) {
        set_process(false);
    }
}

int64_t CompoundCloudPlane::get_native_instance() {
    return instance_id;
}

void CompoundCloudPlane::init(int render_priority, int cloud1, int cloud2, int cloud3, int cloud4) {
    compounds.resize(4);
    compounds.set(0, cloud1);
    compounds.set(1, cloud2);
    compounds.set(2, cloud3);
    compounds.set(3, cloud4);

    set_process_priority(render_priority);

    resolution = 84;
    size = 104;

    if (size <= 0) {
        godot::UtilityFunctions::printerr("Invalid size for CompoundCloudPlane");
        return;
    }

    density.resize(size, std::vector<godot::Vector4>(size, godot::Vector4()));
    old_density.resize(size, std::vector<godot::Vector4>(size, godot::Vector4()));

    create_mesh();
    update_compound_colors();
    update_cloud_texture();
    update_shader_parameters();
}

void CompoundCloudPlane::create_mesh() {
    godot::Ref<godot::QuadMesh> quad_mesh;
    quad_mesh.instantiate();
    quad_mesh->set_size(godot::Vector2(static_cast<float>(size * resolution), static_cast<float>(size * resolution)));
    set_mesh(quad_mesh);
}

void CompoundCloudPlane::create_shader_material() {
    material.instantiate();
    auto shader = godot::ResourceLoader::get_singleton()->load("res://shaders/CompoundCloudPlane.gdshader");
    material->set_shader(godot::Object::cast_to<godot::Shader>(shader.ptr()));
    set_material_override(material);
}

void CompoundCloudPlane::create_noise_texture() {
    noise_texture.instantiate();
    godot::Ref<godot::FastNoiseLite> noise;
    noise.instantiate();
    noise->set_noise_type(godot::FastNoiseLite::TYPE_PERLIN);
    noise->set_frequency(static_cast<float>(0.1));
    noise_texture->set_width(256);
    noise_texture->set_height(256);
    noise_texture->set_noise(noise);
    noise_texture->set_seamless(true);
    noise_texture->set_as_normal_map(false);
    noise_texture->set_bump_strength(4.0);

    material->set_shader_parameter("noise", noise_texture);
}

void CompoundCloudPlane::update_cloud_texture() {
    if (!is_inside_tree() || size <= 0)
        return;

    if (!densities_texture.is_valid()) {
        densities_texture.instantiate();
    }

    godot::Ref<godot::Image> image = godot::Image::create(size, size, false, godot::Image::FORMAT_RGBA8);
    if (!image.is_valid()) {
        godot::UtilityFunctions::printerr("Failed to create valid image for densities_texture");
        return;
    }

    std::lock_guard<std::mutex> lock(density_mutex);

    for (int x = 0; x < size; ++x) {
        for (int y = 0; y < size; ++y) {
            godot::Color pixel_color = godot::Color(0, 0, 0, 0);
            float total_density = 0.0f;
            int dominant_compound = -1;
            float max_density = 0.0f;

            for (int i = 0; i < CLOUDS_IN_ONE; ++i) {
                if (compounds[i] != -1) {
                    float compound_density = density[x][y][i];
                    total_density += compound_density;
                    if (compound_density > max_density) {
                        max_density = compound_density;
                        dominant_compound = i;
                    }
                }
            }

            if (total_density > minimum_visible_density && dominant_compound != -1) {
                pixel_color = compound_colors[dominant_compound];
                pixel_color.a = godot::Math::clamp(total_density * 2.0f, 0.0f, 1.0f); 
            }

            image->set_pixel(x, y, pixel_color);
        }
    }

    densities_texture->set_image(image);
    material->set_shader_parameter("densities", densities_texture);
}

void CompoundCloudPlane::update_shader_parameters() {
    material->set_shader_parameter("UVOffset", position);
    material->set_shader_parameter("BrightnessMultiplier", brightness);
    material->set_shader_parameter("NoiseScale", 14.0f);
    material->set_shader_parameter("CLOUD_SPEED", 0.013f);
}

void CompoundCloudPlane::update_position(const godot::Vector2& new_position) {
    position = new_position;
    cached_world_position = get_global_transform().origin;
    update_shader_parameters();
}

void CompoundCloudPlane::set_brightness(float new_brightness) {
    brightness = new_brightness;
    update_shader_parameters();
}

void CompoundCloudPlane::add_cloud(int compound, int x, int y, float new_density) {
    if (x < 0 || x >= size || y < 0 || y >= size || size <= 0)
        return;

    int index = -1;
    for (int i = 0; i < CLOUDS_IN_ONE; ++i) {
        if (compounds[i] == compound) {
            index = i;
            break;
        }
    }

    if (index == -1)
        return;

    std::lock_guard<std::mutex> lock(density_mutex);
    density[x][y][index] += new_density;
    
    texture_needs_update = true;
    set_process(true);
}

bool CompoundCloudPlane::contains_position(const godot::Vector3& world_position) const {
    godot::Vector3 local_pos = world_position - cached_world_position;
    float half_size = size * resolution / 2.0f;
    return std::abs(local_pos.x) <= half_size && std::abs(local_pos.z) <= half_size;
}

godot::Vector2 CompoundCloudPlane::convert_to_cloud_local(const godot::Vector3& world_position) const {
    godot::Vector3 local_pos = world_position - cached_world_position;
    float half_size = static_cast<float>(size * resolution) / 2.0f;
    float x = (local_pos.x + half_size) / static_cast<float>(resolution);
    float y = (local_pos.z + half_size) / static_cast<float>(resolution);
    return godot::Vector2(
        static_cast<float>(godot::Math::clamp(static_cast<int>(x), 0, size - 1)),
        static_cast<float>(godot::Math::clamp(static_cast<int>(y), 0, size - 1))
    );
}

void CompoundCloudPlane::clear_contents() {
    std::lock_guard<std::mutex> lock(density_mutex);
    for (auto& row : density) {
        for (auto& cell : row) {
            cell = godot::Vector4(0, 0, 0, 0);
        }
    }
    texture_needs_update = true;
    set_process(false);
}

float CompoundCloudPlane::take_compound(int compound, int x, int y, float fraction) {
    if (x < 0 || x >= size || y < 0 || y >= size || fraction < 0)
        return 0.0f;

    int index = -1;
    for (int i = 0; i < CLOUDS_IN_ONE; ++i) {
        if (compounds[i] == compound) {
            index = i;
            break;
        }
    }

    if (index == -1)
        return 0.0f;

    std::lock_guard<std::mutex> lock(density_mutex);
    float amount = density[x][y][index] * fraction;
    density[x][y][index] -= amount;
    
    if (density[x][y][index] < minimum_visible_density)
        density[x][y][index] = 0;

    texture_needs_update = true;
    return amount;
}

float CompoundCloudPlane::amount_available(int compound, int x, int y, float fraction) const {
    if (x < 0 || x >= size || y < 0 || y >= size || fraction < 0)
        return 0.0f;

    int index = -1;
    for (int i = 0; i < CLOUDS_IN_ONE; ++i) {
        if (compounds[i] == compound) {
            index = i;
            break;
        }
    }

    if (index == -1)
        return 0.0f;

    std::lock_guard<std::mutex> lock(density_mutex);
    float amount = density[x][y][index] * fraction;
    return amount > minimum_visible_density ? amount : 0.0f;
}

bool CompoundCloudPlane::contains_position_with_radius(const godot::Vector3& world_position, float radius) const {
    godot::Vector3 local_pos = world_position - cached_world_position;
    float half_size = size * resolution / 2.0f;
    return std::abs(local_pos.x) <= half_size + radius && std::abs(local_pos.z) <= half_size + radius;
}

godot::Vector3 CompoundCloudPlane::convert_to_world(int cloud_x, int cloud_y) const {
    float half_size = size * resolution / 2.0f;
    float x = cloud_x * resolution - half_size;
    float z = cloud_y * resolution - half_size;
    return cached_world_position + godot::Vector3(x, 0, z);
}

godot::PackedInt32Array CompoundCloudPlane::get_compounds() const {
    return compounds;
}

void CompoundCloudPlane::set_compounds(const godot::PackedInt32Array& new_compounds) {
    compounds = new_compounds;
    update_compound_colors();
}

int CompoundCloudPlane::get_resolution() const {
    return resolution;
}

void CompoundCloudPlane::set_resolution(int new_resolution) {
    resolution = new_resolution;
    create_mesh();
}

int CompoundCloudPlane::get_size() const {
    return size;
}

void CompoundCloudPlane::set_size(int new_size) {
    size = new_size;
    create_mesh();

    std::lock_guard<std::mutex> lock(density_mutex);
    density.resize(size, std::vector<godot::Vector4>(size, godot::Vector4()));
    old_density.resize(size, std::vector<godot::Vector4>(size, godot::Vector4()));
}

void CompoundCloudPlane::diffuse(float delta) {
    float diffusion_rate = 0.01f * delta; // Significantly reduced

    std::lock_guard<std::mutex> lock(density_mutex);
    for (int x = 0; x < size; ++x) {
        for (int y = 0; y < size; ++y) {
            for (int i = 0; i < CLOUDS_IN_ONE; ++i) {
                if (density[x][y][i] > minimum_visible_density) {
                    float sum = 0.0f;
                    int count = 0;
                    for (int dx = -1; dx <= 1; dx++) {
                        for (int dy = -1; dy <= 1; dy++) {
                            int nx = (x + dx + size) % size;
                            int ny = (y + dy + size) % size;
                            sum += density[nx][ny][i];
                            count++;
                        }
                    }
                    float avg = sum / count;
                    float new_density = density[x][y][i] + (avg - density[x][y][i]) * diffusion_rate;
                    old_density[x][y][i] = new_density * (1.0f - (1.0f - decay_rates[i]) * delta);
                    
                    if (old_density[x][y][i] < minimum_visible_density)
                        old_density[x][y][i] = 0;
                }
                else {
                    old_density[x][y][i] = 0;
                }
            }
        }
    }
    texture_needs_update = true;
}

void CompoundCloudPlane::advect(float delta) {
    std::lock_guard<std::mutex> lock(density_mutex);
    std::swap(density, old_density);

    float flow_speed = 0.05f * delta; 
    for (int x = 0; x < size; ++x) {
        for (int y = 0; y < size; ++y) {
            float new_x = std::fmod(static_cast<float>(x) + flow_speed, static_cast<float>(size));
            float new_y = std::fmod(static_cast<float>(y) + flow_speed, static_cast<float>(size));
            int nx = static_cast<int>(new_x);
            int ny = static_cast<int>(new_y);
            
            // Bilinear interpolation
            float fx = new_x - nx;
            float fy = new_y - ny;
            int nx1 = (nx + 1) % size;
            int ny1 = (ny + 1) % size;
            
            for (int i = 0; i < CLOUDS_IN_ONE; ++i) {
                if (old_density[x][y][i] > minimum_visible_density) {
                    float interpolated_density = 
                        old_density[x][y][i] * ((1-fx)*(1-fy)) +
                        old_density[nx1][y][i] * (fx*(1-fy)) +
                        old_density[nx][ny1][i] * ((1-fx)*fy) +
                        old_density[nx1][ny1][i] * (fx*fy);
                    
                    density[nx][ny][i] = interpolated_density;
                }
                else {
                    density[nx][ny][i] = 0;
                }
            }
        }
    }
    texture_needs_update = true;
}

bool CompoundCloudPlane::has_compounds() const {
    std::lock_guard<std::mutex> lock(density_mutex);
    for (const auto& row : density) {
        for (const auto& cell : row) {
            if (cell.length_squared() > minimum_visible_density * minimum_visible_density) {
                return true;
            }
        }
    }
    return false;
}

void CompoundCloudPlane::update_texture() {
    texture_needs_update = true;
}

godot::PackedInt32Array CompoundCloudPlane::get_compounds_array() const {
    return compounds;
}

int CompoundCloudPlane::get_compounds_count() const {
    return static_cast<int>(compounds.size());
}

void CompoundCloudPlane::update_compound_colors() {
    // Define a map of compound IDs to their corresponding colors from compounds.json
    std::unordered_map<int, godot::Color> compound_color_map = {
        {2, godot::Color(1.0f, 0.4f, 0.1f, 1.0f)},  // Ammonia
        {3, godot::Color(0.8f, 0.4f, 1.0f, 1.0f)},  // Phosphates
        {4, godot::Color(0.949f, 0.918f, 0.004f, 1.0f)},  // Hydrogensulfide
        {5, godot::Color(0.9f, 0.9f, 0.9f, 1.0f)},  // Glucose
        {7, godot::Color(0.0f, 1.0f, 0.8f, 1.0f)},  // Mucilage
        {8, godot::Color(0.631f, 0.122f, 0.004f, 1.0f)},  // Iron
    };

    for (int i = 0; i < CLOUDS_IN_ONE; ++i) {
        int compound_id = compounds[i];
        if (compound_id != -1 && compound_color_map.find(compound_id) != compound_color_map.end()) {
            compound_colors[i] = compound_color_map[compound_id];
        } else {
            compound_colors[i] = godot::Color(0, 0, 0, 0);  // Transparent if compound not found
        }
    }

    // Update shader parameters for colors
    for (int i = 0; i < CLOUDS_IN_ONE; ++i) {
        material->set_shader_parameter(godot::String("colour") + godot::String::num_int64(i + 1), compound_colors[i]);
    }
}

} // namespace Thrive
