using BlueSky.Data;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FeatureDataset", menuName = "ScriptableObjects/FeatureDataset", order = 2)]
public class FeatureDataset : ScriptableObject
{
    [SerializeField] private List<Features> features;

    public void SetFeatures(List<Features> features)
    {
        this.features = features;
    }

    public List<Features> GetFeatures()
    {
        return this.features;
    }
}
