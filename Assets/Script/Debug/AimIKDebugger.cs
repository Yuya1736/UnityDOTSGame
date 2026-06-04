using UnityEngine;
using RootMotion.FinalIK;

public class AimIKDebugger : MonoBehaviour
{
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F1)) return;
        var aim = GetComponent<AimIK>();
        if (aim == null) { Debug.LogError("No AimIK on this object"); return; }

        Debug.Log($"AimIK enabled={aim.enabled}  IKPositionWeight={aim.solver.IKPositionWeight:F3}");
        Debug.Log($"solver.transform={aim.solver.transform?.name}  axis={aim.solver.axis}  IKPos={aim.solver.IKPosition}");
        for (int i = 0; i < aim.solver.bones.Length; i++)
            Debug.Log($"  bone[{i}] name={aim.solver.bones[i].transform?.name}  weight={aim.solver.bones[i].weight:F3}");
    }
}
