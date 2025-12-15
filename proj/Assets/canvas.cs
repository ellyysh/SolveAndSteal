using UnityEngine;

public class LoreScreenController : MonoBehaviour
{
    public GameObject START; 

    void Start()
    {
        START.SetActive(true); 
    }

    public void OnClickStart()
    {
        START.SetActive(false);
    }
}
