using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BlockTypesList", menuName = "ScriptableObjects/BlockTypesList")]
public class BlockTypesListSO : ScriptableObject
{
    public List<GameObject> blockTypes;
}
