using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiBoatSpawner : MonoBehaviour
{
    [SerializeField] GameObject kAiPrefab = null;
    private float count_down_ = 0f;

	// Use this for initialization
	void Start ()
    {
        count_down_ = 0f;
	}
	
	// Update is called once per frame
	void Update ()
    {
        count_down_ -= Time.deltaTime;
        if(count_down_ <= 0f)
        {
            count_down_ = Random.Range(0.75f, 1.75f);
            var player = WaveManager.Instance.Player();
            if (player == null || WaveManager.Instance.EnemyCount() >= 50) return;

            Vector3 position = new Vector3();
            position.x = Random.Range(15f, 30f) * (Random.Range(0, 2) * 2f - 1f);
            position.z = Random.Range(15f, 30f) * (Random.Range(0, 2) * 2f - 1f);
            position += player.transform.position;
            GameObject.Instantiate(kAiPrefab, position, Quaternion.identity);
        }
	}
}
