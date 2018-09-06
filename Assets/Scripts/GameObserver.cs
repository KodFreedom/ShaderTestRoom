using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectBaka;

public class GameObserver : MonoBehaviour
{
    /////////////////////////////////////////////////////////////////////////
    // シングルトーンインスタンス
    /////////////////////////////////////////////////////////////////////////
    private static GameObserver instance_ = null;
    public static GameObserver Instance { get { return instance_; } }

    private float time_counter_ = 0f;
    private bool end_ = false;

    public float CurrentTime()
    {
        return time_counter_;
    }

    public void GameOver(BoatController player)
    {
        if (end_ == true || player == null) return;
        end_ = true;
        GameFlowController.Instance.GameOver();
    }

    private void GameClear()
    {
        if (end_ == true) return;
        end_ = true;
        GameFlowController.Instance.GameClear();
    }

    private void Awake()
    {
        // インスタンスが生成されてるかどうかをチェックする
        if (null == instance_)
        {
            // ないなら自分を渡す
            instance_ = this;
        }
        else
        {
            // すでにあるなら自分を破棄する
            DestroyImmediate(gameObject);
            return;
        }
    }

    // Use this for initialization
    void Start ()
    {
        time_counter_ = 0f;
        end_ = false;
    }
	
	// Update is called once per frame
	void Update ()
    {
        time_counter_ += Time.deltaTime;
        if(time_counter_ >= 30f)
        {
            time_counter_ = 30f;
            GameClear();
        }
	}
}
