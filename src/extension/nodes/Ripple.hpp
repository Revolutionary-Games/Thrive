#pragma once

#include <godot_cpp/classes/mesh.hpp>
#include <godot_cpp/classes/node3d.hpp>
#include <godot_cpp/classes/mesh_instance3d.hpp>
#include <godot_cpp/classes/material.hpp>
#include <godot_cpp/classes/cpu_particles3d.hpp>
#include <godot_cpp/classes/immediate_mesh.hpp>
#include <godot_cpp/classes/standard_material3d.hpp>
#include <godot_cpp/core/class_db.hpp>

namespace Thrive {

/**
 * The Ripple class creates a visual trails that follow moving microbes,
 * gradually fading away like the name suggest, ripples in water.
 */
class Ripple : public godot::Node3D {
    GDCLASS(Ripple, godot::Node3D)

private:
    /**
     * Holds the data for a single point in our ripple trail
     */
    struct RipplePoint {
        godot::Vector3 position; ///< where does this point sit in the world?
        float age;              ///< how long this point has existed or used for fading?
    };

    /// Keep track of this many points 
    static constexpr size_t MAX_POINTS = 20;
    
    /// All our ripple points live here
    RipplePoint points[MAX_POINTS];
    
    /// How many points are currently active
    int numPoints = 0;

    /// How wide our ripple effect should be 
    float rippleWidth = 0.12f;
    
    /// How long each ripple point sticks around before fading away
    float lifetime = 0.3f;
    
    /// Don't create new points unless we've moved at least this far
    static constexpr float MIN_POINT_DISTANCE = 0.01f;
    
    /// The 3D object that shows our ripple effect
    godot::MeshInstance3D* meshInstance = nullptr;
    
    /// The mesh we update dynamically as ripples form
    godot::Ref<godot::ImmediateMesh> mesh;
    
    /// The material that controls how our ripples look
    godot::Ref<godot::Material> material;
    
    /// Remember where we were last frame to track movement
    godot::Vector3 lastPosition;
    
    /// Connection to the particle system we share with other effects
    godot::CPUParticles3D* sharedParticles = nullptr;
    
    /// Keep track of our setup state
    bool initialized = false;
    bool isExiting = false;

protected:
    static void _bind_methods();

public:
    Ripple();
    ~Ripple();

    void _ready() override;
    void _process(double delta) override;
    void _exit_tree() override;

    /**
     * Changes the material used for our ripple effect
     * @param p_material The new material to use
     */
    void SetMaterial(const godot::Ref<godot::Material>& p_material);

    /**
     * Gets the material we're currently using for the ripple effect
     * @return The current material
     */
    godot::Ref<godot::Material> GetMaterial() const;

private:
    /**
     * Updates our mesh to match the current ripple points.
     * Creates a smooth trail using triangle strips.
     */
    void UpdateMeshGeometry();

    /**
     * Keeps the particle system in sync with our movement.
     * Makes sure particles appear in the right place.
     */
    void UpdateParticleEmission();

    /**
     * Handles the lifecycle of our ripple points - adds new ones
     * as we move and removes old ones as they fade out
     * @param delta Time since last frame
     */
    void ManageRipplePoints(float delta);
};

} // namespace Thrive