using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour {

	public enum objectWeight
    {
        light,
        middle,
        heavy
    }


    public objectWeight weightClass;
    public int numbersOfObjects;
    public float minSize;
    public float maxSize;

    public float rangeInX;
    public float rangeInY;
    public float rangeInZ;
    List<GameObject> primitives;

    public bool onlyCubes;
    private void Awake()
    {
        primitives = new List<GameObject>();
    }
    void Start () {

        int randomNumberRange = Enum.GetNames(typeof(objectWeight)).Length;

        if (onlyCubes)
            randomNumberRange = 0;

        for(int i = 0; i < numbersOfObjects; ++i)
        {
            int randomNumber = UnityEngine.Random.Range(0, randomNumberRange);

            float randomSizex = UnityEngine.Random.Range(minSize, maxSize);
            float randomSizey = UnityEngine.Random.Range(minSize, maxSize);
            float randomSizez = UnityEngine.Random.Range(minSize, maxSize);

            float randomPositionx = UnityEngine.Random.Range(-rangeInX, rangeInX);
            float randomPositiony = UnityEngine.Random.Range(-rangeInY, rangeInY);
            float randomPositionz = UnityEngine.Random.Range(-rangeInZ, rangeInZ);

            switch (randomNumber)
            {
                case 0:
                    primitives.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
                    primitives[i].transform.parent = this.transform;
                    primitives[i].transform.localPosition = new Vector3(randomPositionx, randomPositiony, randomPositionz);
                    primitives[i].transform.localScale = new Vector3(randomSizex, randomSizey, randomSizez);
                    primitives[i].transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f));
                    break;
                case 1:
                    primitives.Add(GameObject.CreatePrimitive(PrimitiveType.Sphere));
                    primitives[i].transform.parent = this.transform;
                    primitives[i].transform.localPosition = new Vector3(randomPositionx, randomPositiony, randomPositionz);
                    primitives[i].transform.localScale = new Vector3(randomSizex, randomSizey, randomSizez);
                    primitives[i].transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f));
                    break;
                case 2:
                    primitives.Add(GameObject.CreatePrimitive(PrimitiveType.Capsule));
                    primitives[i].transform.parent = this.transform;
                    primitives[i].transform.localPosition = new Vector3(randomPositionx, randomPositiony, randomPositionz);
                    primitives[i].transform.localScale = new Vector3(randomSizex, randomSizey, randomSizez);
                    primitives[i].transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f));
                    break;

            }
        }

        for(int i = 0; i < primitives.Count; ++i)
        {
            primitives[i].AddComponent<Rigidbody>();

            Rigidbody rigidbody = primitives[i].GetComponent<Rigidbody>();
            Renderer r = primitives[i].GetComponent<Renderer>();
            Color myColor;
            switch (weightClass)
            {
                case objectWeight.light:
                    rigidbody.mass = 0.1f;
                    rigidbody.drag = 2f;
                    rigidbody.angularDrag = 1000;
                    rigidbody.useGravity = true;
                    rigidbody.isKinematic = false;
                    myColor = new Color();
                    ColorUtility.TryParseHtmlString("#007E00", out myColor);
                    r.material.color = myColor;
                    break;
                case objectWeight.middle:
                    rigidbody.mass = 0.5f;
                    rigidbody.drag = 2f;
                    rigidbody.angularDrag = 1000;
                    rigidbody.useGravity = true;
                    rigidbody.isKinematic = false;
                    myColor = new Color();
                    ColorUtility.TryParseHtmlString("#7E0007", out myColor);
                    r.material.color = myColor;
                    break;
                case objectWeight.heavy:
                    rigidbody.mass = 1f;
                    rigidbody.drag = 2f;
                    rigidbody.angularDrag = 1000;
                    rigidbody.useGravity = true;
                    rigidbody.isKinematic = false;
                    myColor = new Color();
                    ColorUtility.TryParseHtmlString("#333333", out myColor);
                    r.material.color = myColor;
                    break;
            }

        }

	}
	
}
