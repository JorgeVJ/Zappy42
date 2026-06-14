using Godot;
using System.Collections.Generic;

/// <summary>
/// Base genérica para managers que indexan entidades de tipo <typeparamref name="T"/>
/// por un ID numérico y las mantienen como hijas de un contenedor <see cref="Node3D"/>.
/// Centraliza el diccionario, el contenedor y las operaciones comunes
/// (<see cref="TryGet"/>, <see cref="Remove"/>, registro al crear).
/// </summary>
public partial class EntityManager<T> : Node where T : Node3D
{
    /// <summary>Entidades activas indexadas por ID.</summary>
    protected readonly Dictionary<int, T> entities = new();

    /// <summary>Contenedor donde se añaden/eliminan las instancias como hijos.</summary>
    protected Node3D container;

    /// <summary>Nombre del nodo contenedor (se crea si no existe). Override en cada manager.</summary>
    protected virtual string ContainerName => "Entities";

    public override void _Ready()
    {
        container = GetNodeOrNull<Node3D>(ContainerName);
        if (container == null)
        {
            container = new Node3D();
            container.Name = ContainerName;
            AddChild(container);
        }
    }

    /// <summary>Registra una entidad ya creada: la añade al contenedor y al diccionario.</summary>
    protected T Register(int id, T entity)
    {
        container.AddChild(entity);
        entities[id] = entity;
        return entity;
    }

    public bool TryGet(int id, out T entity) => entities.TryGetValue(id, out entity);

    public void Remove(int id)
    {
        if (!entities.TryGetValue(id, out var entity))
            return;

        entity.QueueFree();
        entities.Remove(id);
    }
}
