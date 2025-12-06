using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for Button component

public class ButtonTest : MonoBehaviour
{
    public Button button; // Reference to the button
    public GameObject targetObject; // Object to change color

    void Start()
    {
        // Ensure the button is assigned and add a listener to it
        if (button != null)
        {
            button.onClick.AddListener(ChangeColor);
        }
    }

    void ChangeColor()
    {
        if (targetObject != null)
        {
            // Pick a random color each time the button is clicked
            targetObject.GetComponent<Renderer>().material.color = new Color(Random.value, Random.value, Random.value);
        }
    }
}
