#include "Ripple.hpp"

namespace Thrive {

// Binds methods to be accessible from Godot
void Ripple::_bind_methods() {
    using namespace godot;

    // Method binder
    ClassDB::bind_method(D_METHOD("set_material", "material"), &Ripple::SetMaterial);
    ClassDB::bind_method(D_METHOD("get_material"), &Ripple::GetMaterial);
    
    // Add the material property to make it visible and editable in Godot editor
    ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "material", 
        PROPERTY_HINT_RESOURCE_TYPE, "Material",
        PROPERTY_USAGE_DEFAULT), 
        "set_material", "get_material");
}

// Constructor: Initializes the Ripple effect and enables processing
Ripple::Ripple() {
    // Enable _process to be called every frame
    set_process(true);
}

// Destructor: Cleans up resources when the Ripple effect is destroyed
Ripple::~Ripple() {
    isExiting = true;
    if (meshInstance) {
        meshInstance->queue_free();
    }
}

// Called when the node exits the scene tree
void Ripple::_exit_tree() {
    // clean up references when leaving the scene
    isExiting = true;
    if (sharedParticles) {
        sharedParticles = nullptr;  // don't own this just clear reference
    }
}

// Called when the node enters the scene tree
void Ripple::_ready() {
    // create and set up the immediate mesh for dynamic geometry
    mesh.instantiate();
    meshInstance = memnew(godot::MeshInstance3D);
    add_child(meshInstance);
    meshInstance->set_mesh(mesh);

    // handle material setup
    if (material.is_valid()) {
        // use existing material if one was set
        meshInstance->set_material_override(material);
    } else {
        // create default transparent material if none provided
        godot::Ref<godot::StandardMaterial3D> mat;
        mat.instantiate();
        mat->set_transparency(godot::BaseMaterial3D::TRANSPARENCY_ALPHA);
        mat->set_shading_mode(godot::BaseMaterial3D::SHADING_MODE_UNSHADED);
        mat->set_albedo(godot::Color(1, 0, 0, 0.5));
        SetMaterial(mat);
    }

    // get reference to the shared particle system in the scene
    sharedParticles = get_node<godot::CPUParticles3D>("SharedParticles");
    
    // store initial position for movement tracking
    lastPosition = get_global_position();
    initialized = true;
}

// Called every frame to update the ripple effect
void Ripple::_process(double delta) {
    // skip processing if not properly initialized or being destroyed
    if (!initialized || !meshInstance || isExiting) 
        return;

    // update ripple points, mesh geometry and particle emission
    ManageRipplePoints(static_cast<float>(delta));
    UpdateMeshGeometry();
    UpdateParticleEmission();
}

// Manages the array of points that form the ripple trail
void Ripple::ManageRipplePoints(float delta) {
    // get current position in world space
    godot::Vector3 currentPosition = get_global_position();
    
    // only create new points when movement threshold is exceeded
    if ((currentPosition - lastPosition).length() > MIN_POINT_DISTANCE) {
        // shift existing points if at capacity
        if (numPoints >= MAX_POINTS) {
            // move all points one position back discarding the oldest
            for (int i = 0; i < MAX_POINTS - 1; i++) {
                points[i] = points[i + 1];
            }
            numPoints = MAX_POINTS - 1;
        }
        
        // add new point at current position
        points[numPoints].position = currentPosition;
        points[numPoints].age = 0;
        numPoints++;
        
        // update last position for next frame's comparison
        lastPosition = currentPosition;
    }

    // update ages and remove expired points
    int alivePoints = 0;
    for (int i = 0; i < numPoints; i++) {
        // increment age of point
        points[i].age += delta;
        
        // keep point if still within lifetime
        if (points[i].age < lifetime) {
            // compact array by moving valid points to front if needed
            if (i != alivePoints) {
                points[alivePoints] = points[i];
            }
            alivePoints++;
        }
    }
    // update count of valid points
    numPoints = alivePoints;
}

// Updates the mesh geometry based on current ripple points
void Ripple::UpdateMeshGeometry() {
    // clear previous geometry
    mesh->clear_surfaces();
    
    // need at least 2 points to create geometry
    if (numPoints < 2) 
        return;

    // begin creating triangle strip for the ripple trail
    mesh->surface_begin(godot::Mesh::PRIMITIVE_TRIANGLE_STRIP);
    
    for (int i = 0; i < numPoints; i++) {
        // calculate fade based on point age
        float alpha = 1.0f - (points[i].age / lifetime);
        godot::Color color(1, 0, 0, alpha);

        // calculate direction vector between points
        godot::Vector3 direction;
        if (i < numPoints - 1) {
            // use direction to next point for all except last point
            direction = (points[i + 1].position - points[i].position).normalized();
        } else {
            // use direction from previous point for last point
            direction = (points[i].position - points[i - 1].position).normalized();
        }

        // cross product with up vector creates perpendicular vector
        godot::Vector3 side = direction.cross(godot::Vector3(0, 1, 0)).normalized() * rippleWidth;

        // add vertices for both sides of the trail
        mesh->surface_set_color(color);
        mesh->surface_add_vertex(to_local(points[i].position + side));
        mesh->surface_set_color(color);
        mesh->surface_add_vertex(to_local(points[i].position - side));
    }

    // Finish the mesh surface
    mesh->surface_end();
}

// Updates the shared particle system's emission position
void Ripple::UpdateParticleEmission() {
    // only update particles if we have a valid particle system and have moved enough
    if (sharedParticles && (lastPosition - get_global_position()).length() > MIN_POINT_DISTANCE) {
        // enable emission and update particle system position
        sharedParticles->set_emitting(true);
        sharedParticles->set_global_position(get_global_position());
    }
}

// Sets the material used for rendering the ripple effect
void Ripple::SetMaterial(const godot::Ref<godot::Material>& p_material) {
    // store the material reference
    material = p_material;
    
    // apply material immediately if mesh instance exists
    if (meshInstance) {
        meshInstance->set_material_override(material);
    }
}

// Returns the current material used by the ripple effect
godot::Ref<godot::Material> Ripple::GetMaterial() const {
    return material;
}

} // namespace Thrive