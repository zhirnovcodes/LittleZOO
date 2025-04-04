using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zoo.Enums;
[System.Serializable]
public class ActionChainItem
{
    public ActionTypes ActionType;
    public List<SubActionTypes> SubActions;
}

// ScriptableObject to configure simulation settings in the Unity Editor
[CreateAssetMenu(fileName = "ActionChainConfig", menuName = "Simulation/ActionChain")]
public class ActionChainConfigAsset : ScriptableObject
{
    public List<ActionChainItem> Actions;

    public BlobAssetReference<ActionChainSettings> ToSettings()
    {
        var builder = new BlobBuilder(Allocator.Temp);

        ref var root = ref builder.ConstructRoot<ActionChainSettings>();

        var maxSubActionsCount = 0;
        for (int i = 0; i < Actions.Count; i++)
        {
            maxSubActionsCount = Mathf.Max(Actions[i].SubActions.Count, maxSubActionsCount);
        }
        
        int actionsCount = System.Enum.GetNames(typeof(ActionTypes)).Length;

        root.ActionsCount = actionsCount;
        root.SubActionsCount = maxSubActionsCount;

        var nodearray = builder.Allocate(ref root.ActionsMap, maxSubActionsCount * actionsCount);

        for (int i = 0; i < Actions.Count; i++)
        {
            for (var j = 0; j < maxSubActionsCount; j++)
            {
                nodearray[i * maxSubActionsCount + j] = (j < Actions[i].SubActions.Count) ? (int)Actions[i].SubActions[j] : -1;
            }
        }

        var result = builder.CreateBlobAssetReference<ActionChainSettings>(Allocator.Persistent);

        return result;
    }
}
