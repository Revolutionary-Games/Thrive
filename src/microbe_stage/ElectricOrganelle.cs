using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using Components;

namespace Thrive.MicrobeStage.Components;

// Мы используем struct (структуру), так как Thrive работает на Arch ECS.
// Это именно то, что требовали разработчики в doc/architecture.md.
public struct ElectricOrganelle : IComponent
{
    // Параметры органеллы (данные)
    public float Damage { get; set; }
    public float Range { get; set; }
    public float AtpCost { get; set; }
    public float Cooldown { get; set; }
    
    // Таймер перезарядки
    public float CurrentCooldownTimer { get; set; }

    // Конструктор для начальных значений
    public ElectricOrganelle()
    {
        Damage = 20.0f;
        Range = 150.0f;
        AtpCost = 10.0f;
        Cooldown = 2.0f;
        CurrentCooldownTimer = 0.0f;
    }
}
