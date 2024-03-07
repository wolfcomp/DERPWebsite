using System.Collections;
using System.Text;

namespace DERPWebsite;

/// <summary>
/// This implements an algorithm for sampling from a discrete probability distribution via a generic list
/// with extremely fast O(1) get operations and small (close to minimally small) O(n) space complexity and
/// O(n) CRUD complexity. In other words, you can add any item of type T to a List with an integer weight,
/// and get a random item from the list with probability ( weight / sum-weights ).
/// </summary>
public class WeightedList<T> : IEnumerable<T>
{
    /// <summary>
    /// Create a new WeightedList with an optional System.Random.
    /// </summary>
    /// <param name="rand"></param>
    public WeightedList(Random? rand = null)
    {
        _rand = rand ?? _rand;
    }

    /// <summary>
    /// Create a WeightedList with the provided items and an optional System.Random.
    /// </summary>
    public WeightedList(ICollection<(T item, uint weight)> listItems, Random? rand = null)
    {
        _rand = rand ?? _rand;
        foreach (var (item, weight) in listItems)
        {
            _list.Add(item);
            _weights.Add(weight);
        }
        Recalculate();
    }

    public WeightErrorHandlingType BadWeightErrorHandling { get; set; } = WeightErrorHandlingType.SetWeightToOne;

    public T Next()
    {
        if (Count == 0) return default!;
        var nextInt = _rand.Next(Count);
        if (_areAllProbabilitiesIdentical) return _list[nextInt];
        var nextProbability = _rand.Next(_totalWeight);
        return (nextProbability < _probabilities[nextInt]) ? _list[nextInt] : _list[_alias[nextInt]];
    }

    public void AddWeightToAll(uint weight)
    {
        if (weight + _minWeight <= 0 && BadWeightErrorHandling == WeightErrorHandlingType.ThrowExceptionOnAdd)
            throw new ArgumentException($"Subtracting {-1 * weight} from all items would set weight to non-positive for at least one element.");
        for (var i = 0; i < Count; i++)
        {
            _weights[i] = FixWeight(_weights[i] + weight);
        }
        Recalculate();
    }

    public void SetWeightOfAll(uint weight)
    {
        if (weight <= 0 && BadWeightErrorHandling == WeightErrorHandlingType.ThrowExceptionOnAdd) throw new ArgumentException("Weight cannot be non-positive.");
        for (var i = 0; i < Count; i++) _weights[i] = FixWeight(weight);
        Recalculate();
    }

    public uint TotalWeight => _totalWeight;

    /// <summary>
    /// Minimum weight in the structure. 0 if Count == 0.
    /// </summary>
    public uint MinWeight => _minWeight;

    /// <summary>
    /// Maximum weight in the structure. 0 if Count == 0.
    /// </summary>
    public uint MaxWeight => _maxWeight;

    public IReadOnlyList<T> Items => _list.AsReadOnly();

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

    public void Add(T item, uint weight)
    {
        _list.Add(item);
        _weights.Add(FixWeight(weight));
        Recalculate();
    }

    public void Add(ICollection<WeightedListItem<T>> listItems)
    {
        foreach (var listItem in listItems)
        {
            _list.Add(listItem._item);
            _weights.Add(FixWeight(listItem._weight));
        }
        Recalculate();
    }

    public void Clear()
    {
        _list.Clear();
        _weights.Clear();
        Recalculate();
    }

    public bool Contains(T item) => _list.Contains(item);

    public int IndexOf(T item) => _list.IndexOf(item);

    public void Insert(int index, T item, uint weight)
    {
        _list.Insert(index, item);
        _weights.Insert(index, FixWeight(weight));
        Recalculate();
    }

    public void Remove(T item)
    {
        var index = IndexOf(item);
        RemoveAt(index);
        Recalculate();
    }

    public void RemoveAt(int index)
    {
        _list.RemoveAt(index);
        _weights.RemoveAt(index);
        Recalculate();
    }

    public T this[int index] => _list[index];

    public int Count => _list.Count;

    public void SetWeight(T item, uint newWeight) => SetWeightAtIndex(IndexOf(item), FixWeight(newWeight));

    public uint GetWeightOf(T item) => GetWeightAtIndex(IndexOf(item));

    public void SetWeightAtIndex(int index, uint newWeight)
    {
        _weights[index] = FixWeight(newWeight);
        Recalculate();
    }

    public uint GetWeightAtIndex(int index) => _weights[index];

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("WeightedList<");
        sb.Append(typeof(T).Name);
        sb.Append(">: TotalWeight:");
        sb.Append(TotalWeight);
        sb.Append(", Min:");
        sb.Append(_minWeight);
        sb.Append(", Max:");
        sb.Append(_maxWeight);
        sb.Append(", Count:");
        sb.Append(Count);
        sb.Append(", {");
        for (var i = 0; i < _list.Count; i++)
        {
            sb.Append(_list[i]);
            sb.Append(":");
            sb.Append(_weights[i].ToString());
            if (i < _list.Count - 1) sb.Append(", ");
        }
        sb.Append("}");
        return sb.ToString();
    }

    private readonly List<T> _list = new();
    private readonly List<uint> _weights = new();
    private readonly List<uint> _probabilities = new();
    private readonly List<int> _alias = new();
    private static Random _rand = new();
    private uint _totalWeight;
    private bool _areAllProbabilitiesIdentical;
    private uint _minWeight;
    private uint _maxWeight;

    /// <summary>
    /// https://www.keithschwarz.com/darts-dice-coins/
    /// </summary>
    private void Recalculate()
    {
        _totalWeight = 0;
        _areAllProbabilitiesIdentical = false;
        _minWeight = 0;
        _maxWeight = 0;
        var isFirst = true;

        _alias.Clear(); // STEP 1
        _probabilities.Clear(); // STEP 1

        var scaledProbabilityNumerator = new List<uint>(Count);
        var small = new List<int>(Count); // STEP 2
        var large = new List<int>(Count); // STEP 2
        foreach (uint weight in _weights)
        {
            if (isFirst)
            {
                _minWeight = _maxWeight = weight;
                isFirst = false;
            }
            _minWeight = (weight < _minWeight) ? weight : _minWeight;
            _maxWeight = (_maxWeight < weight) ? weight : _maxWeight;
            _totalWeight += weight;
            scaledProbabilityNumerator.Add(weight * (uint)Count); // STEP 3 
            _alias.Add(0);
            _probabilities.Add(0);
        }

        // Degenerate case, all probabilities are equal.
        if (_minWeight == _maxWeight)
        {
            _areAllProbabilitiesIdentical = true;
            return;
        }

        // STEP 4
        for (var i = 0; i < Count; i++)
        {
            if (scaledProbabilityNumerator[i] < _totalWeight)
                small.Add(i);
            else
                large.Add(i);
        }

        // STEP 5
        while (small.Count > 0 && large.Count > 0)
        {
            var l = small[^1]; // 5.1
            small.RemoveAt(small.Count - 1);
            var g = large[^1]; // 5.2
            large.RemoveAt(large.Count - 1);
            _probabilities[l] = scaledProbabilityNumerator[l]; // 5.3
            _alias[l] = g; // 5.4
            var tmp = scaledProbabilityNumerator[g] + scaledProbabilityNumerator[l] - _totalWeight; // 5.5, even though using ints for this algorithm is stable
            scaledProbabilityNumerator[g] = tmp;
            if (tmp < _totalWeight)
                small.Add(g); // 5.6 the large is now in the small pile
            else
                large.Add(g); // 5.7 add the large back to the large pile
        }

        // STEP 6
        while (large.Count > 0)
        {
            var g = large[^1]; // 6.1
            large.RemoveAt(large.Count - 1);
            _probabilities[g] = _totalWeight; //6.1
        }
    }

    internal static uint FixWeightSetToOne(uint weight) => (weight <= 0) ? 1 : weight;

    internal static uint FixWeightExceptionOnAdd(uint weight) => (weight <= 0) ? throw new ArgumentException("Weight cannot be non-positive") : weight;

    private uint FixWeight(uint weight) => (BadWeightErrorHandling == WeightErrorHandlingType.ThrowExceptionOnAdd) ? FixWeightExceptionOnAdd(weight) : FixWeightSetToOne(weight);
}

/// <summary>
/// A single item for a list with matching T. Create one or more WeightedListItems, add to a Collection
/// and Add() to the WeightedList for a single calculation pass.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct WeightedListItem<T>
{
    internal readonly T _item;
    internal readonly uint _weight;

    public WeightedListItem(T item, uint weight)
    {
        _item = item;
        _weight = weight;
    }
}

public enum WeightErrorHandlingType
{
    SetWeightToOne, // Default
    ThrowExceptionOnAdd, // Throw exception for adding non-positive weight.
}

public static class WeightedListExtensions
{
    public static uint Next(this Random rand, uint max) => (uint)rand.Next((int)max);
}