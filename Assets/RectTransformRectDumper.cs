using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
public class RectTransformRectDumper : MonoBehaviour {
    public void Update() {
        Debug.Log(GetComponent<RectTransform>().rect);
    }
}
