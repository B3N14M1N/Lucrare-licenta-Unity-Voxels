using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerInspector : Editor
{
    /*
    int toolbarInt = 0;
    string[] toolbarStrings = { "Flying", "Walking" };

    private static readonly Color backgroundColor = new Color(0.4f, .4f, .4f, 1f);
    public override void OnInspectorGUI()
    {

        //if (DrawDefaultInspector()) { };

        PlayerController myScript = target as PlayerController;

        EditorGUILayout.LabelField("Camera Settings:");
        EditorGUI.indentLevel++;

        EditorGUILayout.PrefixLabel("Min/Max Y");
        EditorGUILayout.BeginHorizontal();
        myScript.MinY = EditorGUILayout.FloatField(myScript.MinY);
        myScript.MaxY = EditorGUILayout.FloatField(myScript.MaxY);
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.PrefixLabel("Min/Max X");
        EditorGUILayout.BeginHorizontal();
        myScript.MinX = EditorGUILayout.FloatField(myScript.MinX);
        myScript.MaxX = EditorGUILayout.FloatField(myScript.MaxX);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PrefixLabel("Look Sensitivity");
        myScript.LookSensitivity = EditorGUILayout.FloatField(myScript.LookSensitivity);

        myScript.cameraPivot = EditorGUILayout.ObjectField("Camera Pivot", myScript.cameraPivot, typeof(Transform), true) as Transform;


        EditorGUI.indentLevel--;


        EditorGUILayout.Space(5f);

        var screenRect = GUILayoutUtility.GetRect(1, 1);
        var vertRect = EditorGUILayout.BeginVertical();
        EditorGUI.DrawRect(new Rect(screenRect.x - 13, screenRect.y - 1, screenRect.width + 17, vertRect.height + 9), backgroundColor);

        //EditorGUILayout.LabelField("Control Mode Settings:");
        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        if (myScript.stateChanged)
        {
            toolbarInt = GUILayout.Toolbar(((int)myScript.State), toolbarStrings);
            myScript.stateChanged = false;
        }
        if (toolbarInt == 0)
        {
            myScript.State = PlayerControllerState.FlyMode;
            EditorGUILayout.LabelField("Fly Mode Settings:");
            EditorGUI.indentLevel++;
            EditorGUILayout.PrefixLabel("Main Speed");
            myScript.mainSpeed = EditorGUILayout.FloatField(myScript.mainSpeed);
            EditorGUILayout.PrefixLabel("Shift Speed Add");
            myScript.shiftAdd = EditorGUILayout.FloatField(myScript.shiftAdd);
            EditorGUILayout.PrefixLabel("Max Shift Speed");
            myScript.maxShift = EditorGUILayout.FloatField(myScript.maxShift);
            EditorGUI.indentLevel--;
        }
        if (toolbarInt == 1)
        {
            myScript.State = PlayerControllerState.FPSMode;
            EditorGUILayout.LabelField("FPS Mode Settings:");
            EditorGUI.indentLevel++;
            EditorGUILayout.PrefixLabel("Move Speed");
            myScript.MoveSpeed = EditorGUILayout.FloatField(myScript.MoveSpeed);
            EditorGUILayout.PrefixLabel("Sprint Speed");
            myScript.SprintSpeed = EditorGUILayout.FloatField(myScript.SprintSpeed);
            EditorGUILayout.PrefixLabel("Jump Power");
            myScript.JumpPower = EditorGUILayout.FloatField(myScript.JumpPower);
            EditorGUI.indentLevel--;
        }
        if (toolbarInt == 2)
        {
            myScript.State = PlayerControllerState.TPSMode;
            EditorGUILayout.LabelField("TPS Mode Settings:");
            EditorGUI.indentLevel++;
            EditorGUILayout.PrefixLabel("Zoom Sensitivity");
            myScript.ZoomSensitivity = EditorGUILayout.FloatField(myScript.ZoomSensitivity);
            EditorGUILayout.PrefixLabel("Min/Max Zoom");
            EditorGUILayout.BeginHorizontal();
            myScript.minMaxZoom.x = EditorGUILayout.IntField(myScript.minMaxZoom.x);
            myScript.minMaxZoom.y = EditorGUILayout.IntField(myScript.minMaxZoom.y);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(5f);
        EditorGUILayout.EndVertical();
    }
    */
}
    