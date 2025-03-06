using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;  // Needed for sorting

public class TeleportationManager : MonoBehaviour
{
    public CanvasGroup fadeCanvas;  // UI for fade effect (assign in inspector)
    public float teleportDelay = 3f; // Delay before teleporting (currently 3seconds)

    public Material correctMaterial;  // Green Material (Assign in Inspector)
    public Material wrongMaterial;    // Red Material (Assign in Inspector)

    private GameObject player;
    private List<Transform> portalDummyLocations; // Store PortalDummy locations
    private int currentIndex = 0; // Tracks current position in sequence

    void Start()
    {
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player tag not found in the scene!");
            return;
        }

        // Find all "PortalDummy" objects and sort them by name
        portalDummyLocations = GameObject.FindGameObjectsWithTag("DecisionPoint")
            .Select(dp => dp.transform.Find("PortalDummy")) // Find child named "PortalDummy"
            .Where(dummy => dummy != null) // Ensure it exists
            .OrderBy(dummy => ExtractNumber(dummy.name)) // Sort numerically
            .ToList();

        if (portalDummyLocations.Count == 0)
        {
            Debug.LogError("No PortalDummy locations found!");
        }
    }

    public void InteractWithPortal(GameObject portal)
    {
        bool isCorrectPortal = portal.name.Contains("(Correct)");

        Renderer portalRenderer = portal.GetComponent<Renderer>();
        if (portalRenderer == null)
        {
            Debug.LogWarning("Portal Renderer not found.");
            return;
        }

        if (isCorrectPortal)
        {
            portalRenderer.material = correctMaterial; // Turn portal green
            StartCoroutine(TeleportWithDelay());
        }
        else
        {
            portalRenderer.material = wrongMaterial; // Turn portal red
        }
    }

    private IEnumerator TeleportWithDelay()
    {
        yield return StartCoroutine(FadeToBlack()); // 1 sec fade to black
        yield return new WaitForSeconds(1);         // 1 sec black screen
        TeleportToNextPoint();
        yield return StartCoroutine(FadeFromBlack()); // 1 sec fade back
    }

    private void TeleportToNextPoint()
    {
        if (player == null || portalDummyLocations.Count == 0) return;

        if (currentIndex < portalDummyLocations.Count - 1)
        {
            currentIndex++; // Move to the next index
        }
        else
        {
            Debug.Log("Reached the last decision point. No further teleportation.");
            return;
        }

        Transform nextLocation = portalDummyLocations[currentIndex];
        player.transform.position = nextLocation.position;
        Debug.Log("Teleported to: " + nextLocation.name);
    }

    private IEnumerator FadeToBlack()
    {
        float duration = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            fadeCanvas.alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        fadeCanvas.alpha = 1f;
    }

    private IEnumerator FadeFromBlack()
    {
        float duration = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        fadeCanvas.alpha = 0f;
    }

    private int ExtractNumber(string name)
    {
        // Extracts numbers from names like "PortalDummy1", "PortalDummy2", etc.
        string number = new string(name.Where(char.IsDigit).ToArray());
        return int.TryParse(number, out int result) ? result : 0;
    }
}
