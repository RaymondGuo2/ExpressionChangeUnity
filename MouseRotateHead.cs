using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateHeadWithMouse : MonoBehaviour
{
    public Vector2 turn;
    public Rect boundingBox;
    public float sensitivity = 10f;

    void Start() {
        transform.localRotation = Quaternion.Euler(0, 180f, 0);
    }
    void Update() {

        Vector3 mousePosition = Input.mousePosition;

        if (Input.GetMouseButton(2) && mouseInBoundingBox(mousePosition))
        {
            turn.x += Input.GetAxis("Mouse X") * sensitivity;
            turn.y += Input.GetAxis("Mouse Y") * sensitivity;
            transform.rotation = Quaternion.Euler(-turn.y, turn.x + 180f, 0);
        }
    }

    bool mouseInBoundingBox(Vector3 mousePosition)
    {
        return boundingBox.Contains(new Vector2(mousePosition.x, mousePosition.y));
    }

}

