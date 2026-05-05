using Godot;
using System.Collections.Generic;

/// <summary>
/// Electric Organelle – creates an electric field around the microbe upon attack.
/// Scientific basis: electrocytes – specialized cells that generate electric discharges in fish.
/// </summary>
public class ElectricOrganelle : Node2D
{
    #region Configuration

    [Export]
    private float fieldRadius = 100f;                           // Radius of electric field

    [Export]
    private int fieldDamage = 20;                               // Damage per pulse

    [Export]
    private float atpCost = 40f;                                // ATP cost per activation

    [Export]
    private float activationCooldown = 2f;                      // Cooldown in seconds

    #endregion

    #region Private Fields

    private Node2D owner;                                      // The parent microbe
    private bool isOnCooldown = false;
    private bool isFieldActive = false;
    private float currentCooldown = 0f;
    private float fieldPulseTimer = 0f;
    private const float PULSE_INTERVAL = 0.5f;                  // Damage pulse interval

    private Area2D electricFieldArea;
    private CollisionShape2D collisionShape;
    private Sprite2D fieldSprite;

    #endregion

    /// <summary>
    /// Scientific justification:
    /// Electrocytes are specialized cells found in electric fish (e.g., electric eel).
    /// They generate a discharge by synchronously opening voltage-gated ion channels,
    /// which requires ATP to maintain ion gradients. In the game, this is modeled as
    /// ATP consumption to create a damaging electric field around the cell.
    /// </summary>
    public override void _Ready()
    {
        owner = GetParent<Microbe>();
        if (owner == null)
        {
            GD.PrintErr("ElectricOrganelle: organelle must be attached to a microbe!");
            return;
        }

        SetupFieldArea();
        SetupVisualEffects();
        RegisterWithMicrobe();
    }

    private void SetupFieldArea()
    {
        electricFieldArea = new Area2D();
        electricFieldArea.Name = "ElectricFieldArea";

        collisionShape = new CollisionShape2D();
        var circleShape = new CircleShape2D();
        circleShape.Radius = fieldRadius;
        collisionShape.Shape = circleShape;

        electricFieldArea.AddChild(collisionShape);

        // Collision settings: only enemies, not the owner
        electricFieldArea.CollisionLayer = 0;
        electricFieldArea.CollisionMask = 1; // Enemy collision layer

        electricFieldArea.Connect("body_entered", this, nameof(OnBodyEntered));
        electricFieldArea.Connect("body_exited", this, nameof(OnBodyExited));

        AddChild(electricFieldArea);
        
        electricFieldArea.SetDeferred("monitoring", false);
        electricFieldArea.SetDeferred("monitorable", false);
    }

    private void SetupVisualEffects()
    {
        fieldSprite = new Sprite();
        var image = new Image();
        image.Create((int)(fieldRadius * 2), (int)(fieldRadius * 2), false, Image.Format.Rgba8);
        
        for (int x = 0; x < image.GetWidth(); x++)
        {
            for (int y = 0; y < image.GetHeight(); y++)
            {
                float distance = Mathf.Sqrt(Mathf.Pow(x - fieldRadius, 2) + Mathf.Pow(y - fieldRadius, 2));
                if (distance <= fieldRadius)
                {
                    float intensity = 1.0f - (distance / fieldRadius);
                    Color color = new Color(0.5f, 0.8f, 1.0f, intensity * 0.5f);
                    image.SetPixel(x, y, color);
                }
                else
                {
                    image.SetPixel(x, y, Colors.Transparent);
                }
            }
        }
        
        var texture = new ImageTexture();
        texture.CreateFromImage(image);
        fieldSprite.Texture = texture;
        fieldSprite.Modulate = new Color(0.5f, 0.8f, 1.0f, 0.7f);
        fieldSprite.Visible = false;
        
        electricFieldArea.AddChild(fieldSprite);
    }

    private void RegisterWithMicrobe()
    {
        if (owner.HasMethod("RegisterSpecialAbility"))
        {
            owner.Call("RegisterSpecialAbility", this);
        }
    }

    /// <summary>
    /// Main activation method – call this when the attack button is pressed.
    /// </summary>
    public void ActivateElectricField()
    {
        if (isOnCooldown)
        {
            GD.Print("Electric field is on cooldown!");
            return;
        }

        if (!HasEnoughATP(atpCost))
        {
            GD.Print("Not enough ATP to activate electric field!");
            return;
        }

        if (!ConsumeATP(atpCost))
        {
            return;
        }

        StartElectricField();
        StartCooldown();
    }

    private void StartElectricField()
    {
        isFieldActive = true;
        fieldPulseTimer = 0f;
        electricFieldArea.SetDeferred("monitoring", true);
        fieldSprite.Visible = true;
        
        PlayElectricSparkEffect();
        
        GD.Print($"Electric field activated! Radius: {fieldRadius}, Damage: {fieldDamage}");
    }

    private void StopElectricField()
    {
        isFieldActive = false;
        electricFieldArea.SetDeferred("monitoring", false);
        fieldSprite.Visible = false;
    }

    private void StartCooldown()
    {
        isOnCooldown = true;
        currentCooldown = activationCooldown;
    }

    private bool HasEnoughATP(float required)
    {
        if (owner.HasMethod("GetATP"))
        {
            float currentATP = (float)owner.Call("GetATP");
            return currentATP >= required;
        }
        return true; // Fallback for testing without ATP system
    }

    private bool ConsumeATP(float amount)
    {
        if (owner.HasMethod("ConsumeATP"))
        {
            return (bool)owner.Call("ConsumeATP", amount);
        }
        return true;
    }

    private void OnBodyEntered(Node body)
    {
        if (IsEnemy(body))
        {
            DealDamageToTarget(body);
        }
    }

    private void OnBodyExited(Node body)
    {
        // Optional: handle exit
    }

    private bool IsEnemy(Node body)
    {
        Microbe microbe = body as Microbe;
        if (microbe != null && microbe != owner)
        {
            if (owner.HasMethod("IsEnemy") && microbe.HasMethod("IsEnemy"))
            {
                return (bool)owner.Call("IsEnemy", microbe);
            }
            return true;
        }
        return false;
    }

    private void DealDamageToTarget(Node target)
    {
        if (target.HasMethod("TakeDamage"))
        {
            target.Call("TakeDamage", fieldDamage, owner);
            PlayElectricHitEffect(target);
        }
    }

    public override void _Process(double delta)
    {
        if (isFieldActive)
        {
            fieldPulseTimer += delta;
            if (fieldPulseTimer >= PULSE_INTERVAL)
            {
                fieldPulseTimer = 0f;
                ApplyFieldDamage();
            }
        }

        if (isOnCooldown)
        {
            currentCooldown -= delta;
            if (currentCooldown <= 0f)
            {
                isOnCooldown = false;
                StopElectricField();
                GD.Print("Electric field is ready again!");
            }
        }
    }

    private void ApplyFieldDamage()
    {
        foreach (var body in electricFieldArea.GetOverlappingBodies())
        {
            DealDamageToTarget(body);
        }
    }

    private void PlayElectricSparkEffect()
    {
        if (owner.HasMethod("SpawnEffect"))
        {
            owner.Call("SpawnEffect", "electric_spark", GlobalPosition);
        }
    }

    private void PlayElectricHitEffect(Node target)
    {
        if (target.HasMethod("SpawnEffectAt"))
        {
            target.Call("SpawnEffectAt", "electric_hit", target.GetGlobalPosition());
        }
    }

    #region Integration Helpers

    public float GetATPUsage()
    {
        return atpCost;
    }

    public float GetCooldownProgress()
    {
        if (!isOnCooldown) return 1.0f;
        return 1.0f - (currentCooldown / activationCooldown);
    }

    #endregion
}
