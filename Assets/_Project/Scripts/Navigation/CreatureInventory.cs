using System;
using System.Collections.Generic;
using UnityEngine;
using G10.Prototype.Computer;

namespace G10.Prototype.Navigation
{
    /// <summary>Cabin-owned inventory; the expedition timeline persists stable item IDs.</summary>
    public sealed class CreatureInventory : MonoBehaviour, IUpgradeMaterialInventory
    {
        public const int Capacity = 12;
        public sealed class Item
        {
            public string Id { get; }
            public string Name { get; }
            public Texture2D Icon { get; }
            public int Quantity { get; }
            public Item(string id, string name, Texture2D icon, int quantity = 1)
            { Id = id; Name = name; Icon = icon; Quantity = quantity; }
        }
        private readonly List<Item> items = new();
        public IReadOnlyList<Item> Items => items;
        public bool IsFull => items.Count >= Capacity;
        public event Action Changed;
        public event Action ItemAdded;
        public int GetCount(string id) => items.Find(item => item.Id == id)?.Quantity ?? 0;
        public bool CanAdd(string id) => !string.IsNullOrEmpty(id) && (Contains(id) ? GetCount(id) < int.MaxValue : !IsFull);
        public bool Contains(string id) => items.Exists(item => item.Id == id);
        public bool Remove(string id)
        {
            int index = items.FindIndex(item => item.Id == id);
            if (index < 0) return false;
            items.RemoveAt(index); Changed?.Invoke(); return true;
        }
        public void RestoreItems(IEnumerable<Item> restored, bool notify = true)
        {
            var next = new List<Item>(restored);
            var ids = new HashSet<string>();
            if (next.Count > Capacity || next.Exists(item => item == null || string.IsNullOrEmpty(item.Id) || item.Quantity <= 0 || !ids.Add(item.Id)))
                throw new ArgumentException("Invalid inventory stacks.");
            items.Clear(); items.AddRange(next); if (notify) NotifyChanged();
        }
        public bool TryAdd(string id, string name, Texture2D icon)
        {
            if (!CanAdd(id)) return false;
            int index = items.FindIndex(item => item.Id == id);
            if (index < 0) items.Add(new Item(id, name, icon));
            else
            {
                var previous = items[index];
                items[index] = new Item(id, previous.Name, previous.Icon, previous.Quantity + 1);
            }
            ItemAdded?.Invoke();
            NotifyChanged();
            return true;
        }
        public bool TryConsume(UpgradeMaterialRequirement[] requirements)
        {
            var costs = new Dictionary<string, int>();
            foreach (var requirement in requirements ?? Array.Empty<UpgradeMaterialRequirement>())
            {
                if (requirement?.Material == null || string.IsNullOrEmpty(requirement.Material.MaterialId)) return false;
                string id = requirement.Material.MaterialId;
                long total = (long)(costs.TryGetValue(id, out var amount) ? amount : 0) + requirement.RequiredAmount;
                if (total > int.MaxValue) return false;
                costs[id] = (int)total;
            }
            return TryConsume(costs);
        }
        public bool TryConsume(IReadOnlyDictionary<string, int> costs, bool notify = true)
        {
            if (costs == null) return false;
            foreach (var cost in costs)
                if (string.IsNullOrEmpty(cost.Key) || cost.Value <= 0 || GetCount(cost.Key) < cost.Value) return false;
            foreach (var cost in costs)
            {
                int index = items.FindIndex(item => item.Id == cost.Key);
                var item = items[index];
                int remaining = item.Quantity - cost.Value;
                if (remaining == 0) items.RemoveAt(index);
                else items[index] = new Item(item.Id, item.Name, item.Icon, remaining);
            }
            if (notify) NotifyChanged();
            return true;
        }
        public void NotifyChanged() => Changed?.Invoke();
    }
}
