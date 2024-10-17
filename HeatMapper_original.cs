using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatMapper : MonoBehaviour
{
    private float heatMapSpeed = 3.8f;
    private float heatMapRadius = 2.2f;
    float spawnScale = 1f;

    private void Update()
    {
        //Process current gaze point and color it
        RaycastHit gazePoint = ProcessRay(GazeController.gazeOrigin, GazeController.gazeDirection, 0.04f);

        ProcessField(gazePoint);
    }

    public void ProcessField(RaycastHit gazePoint)
    {
        //Also increase coloration of nearby points at slower rates
        if (gazePoint.collider != null && gazePoint.collider.transform != null)
        {
            gazePoint.collider.enabled = false;
            Collider[] hitColliders = Physics.OverlapSphere(
                gazePoint.collider.transform.position, .1f * heatMapRadius);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.name == "GazeParticleSimple(Clone)")
                {
                    ModColors(hitCollider.transform, .01f * heatMapSpeed);
                }
            }

            Collider[] hitColliders2 = Physics.OverlapSphere(
                gazePoint.collider.transform.position, .2f * heatMapRadius);
            foreach (var hitCollider in hitColliders2)
            {
                if (hitCollider.name == "GazeParticleSimple(Clone)")
                {
                    ModColors(hitCollider.transform, .005f * heatMapSpeed);
                }
            }

            gazePoint.collider.enabled = true;
        }
    }

    public void ProcessFieldFromTransform(Vector3 gazePoint)
    {
        //Also increase coloration of nearby points at slower rates
            Collider[] hitColliders = Physics.OverlapSphere(
                gazePoint, .1f * heatMapRadius);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.name == "GazeParticleSimple(Clone)")
                {
                    ModColors(hitCollider.transform, .01f * heatMapSpeed);
                }
            }

            Collider[] hitColliders2 = Physics.OverlapSphere(
                gazePoint, .2f * heatMapRadius);
            foreach (var hitCollider in hitColliders2)
            {
                if (hitCollider.name == "GazeParticleSimple(Clone)")
                {
                    ModColors(hitCollider.transform, .005f * heatMapSpeed);
                }
            }

    }

    public void ProcessPosition(Vector3 position)
    {
            GameObject temp = (GameObject)Instantiate(Resources.Load("GazeParticleSimple"));
            temp.transform.position = position;
            temp.transform.LookAt(Camera.main.transform.position);
            //temp.transform.LookAt(GetComponent<GazeController>().HMD.transform.position);
            temp.transform.Rotate(new Vector3(0, -90, 0));
    }



    //process the gaze to scene ray and surrounding coloration
    public RaycastHit ProcessRay(Vector3 gazeOrigin, Vector3 gazeDirection, float colorDifferential)
    {
        //ignore UI layer (buttons)
        int layerMask = 1 << 5; 
        layerMask = ~layerMask;

        RaycastHit hit;
        if (Physics.Raycast(gazeOrigin, gazeDirection, out hit, 100f, 1))
        {   
            if (hit.collider.name != "GazeParticleSimple(Clone)" && hit.collider.tag != "button")
            {
                GameObject temp = (GameObject)Instantiate(Resources.Load("GazeParticleSimple"));
                temp.transform.position = hit.point;
                temp.transform.LookAt(gazeOrigin);
                temp.transform.Rotate(new Vector3(0, -90, 0));

                if (hit.collider.name == "middlemodel")
                {
                    temp.transform.SetParent(hit.transform, true);
                }
            }
            else if (hit.collider.name == "GazeParticleSimple(Clone)")
            {
                //spawnscale
                ModColors(hit.transform, colorDifferential); 
            }
        }
        return hit;
    }

    //handles the gradual coloration of each point from blue to red 
    public void ModColors(Transform hitTransform, float colorDifferential)
    {
        Color temp = hitTransform.GetComponent<Renderer>().material.color;// .GetColor("_Color");

        float colorMax = 2.55f;

        if (temp.g < colorMax && temp.r < colorMax)
        {
            hitTransform.GetComponent<Renderer>().material.color = new Color(
                temp.r, temp.g + colorDifferential, temp.b, temp.a);
        }
        else if (temp.b > 0)
        {
            hitTransform.GetComponent<Renderer>().material.color = new Color(
                temp.r, temp.g, temp.b - colorDifferential, temp.a);
        }
        else if (temp.r < colorMax)
        {
            hitTransform.GetComponent<Renderer>().material.color = new Color(
                temp.r + colorDifferential, temp.g, temp.b, temp.a);
        }
        else if (temp.g > 0)
        {
            hitTransform.GetComponent<Renderer>().material.color = new Color(
                temp.r, temp.g - colorDifferential, temp.b, temp.a);
        }
    }//end ModColors

}//end HeatMapper.cs