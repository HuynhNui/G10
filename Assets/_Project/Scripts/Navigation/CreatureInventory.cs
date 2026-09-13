using System;
using System.Collections.Generic;
using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Cabin-owned inventory for the current voyage. No disk persistence yet.</summary>
    public sealed class CreatureInventory : MonoBehaviour
    {
        public const int Capacity = 4;
        public sealed class Item
        {
            public string Id { get; }
            public string Name { get; }
            public Texture2D Icon { get; }
            public Item(string id, string name, Texture2D icon) { Id = id; Name = name; Icon = icon; }
        }
        private readonly List<Item> items = new();
        public IReadOnlyList<Item> Items => items;
        public bool IsFull => items.Count >= Capacity;
        public event Action Changed;
        public bool TryAdd(string id, string name, Texture2D icon)
        {
            if (IsFull || string.IsNullOrEmpty(id)) return false;
            foreach (var item in items) if (item.Id == id) return false;
            items.Add(new Item(id, name, icon));
            Changed?.Invoke();
            return true;
        }
    }
}
