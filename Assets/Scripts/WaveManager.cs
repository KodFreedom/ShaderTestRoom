using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    /////////////////////////////////////////////////////////////////////////
    // シングルトーンインスタンス
    /////////////////////////////////////////////////////////////////////////
    private static WaveManager instance_ = null;
    public static WaveManager Instance { get { return instance_; } }

    private static readonly int kMaxObjects = 10;
    private WaterCsController water_ = null;
    private WaveObjectController[] wave_objects_ = new WaveObjectController[kMaxObjects];

    public void Register(WaterCsController water)
    {
        if(water_ == null)
        {
            water_ = water;
        }
    }

    public WaterCsController Water()
    {
        return water_;
    }

    public void Register(WaveObjectController obj)
    {
        for(int i = 0; i < wave_objects_.Length; ++i)
        {
            if(wave_objects_[i] == null)
            {
                wave_objects_[i] = obj;
                break;
            }
        }
    }

    public void Deregister(WaveObjectController obj)
    {
        for (int i = 0; i < wave_objects_.Length; ++i)
        {
            if (wave_objects_[i] == obj)
            {
                wave_objects_[i] = null;
                break;
            }
        }
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

        for (int i = 0; i < wave_objects_.Length; ++i)
        {
            wave_objects_[i] = null;
        }
    }

	// Update is called once per frame
	void LateUpdate ()
    {
        // object更新
        for (int i = 0; i < wave_objects_.Length; ++i)
        {
            if (wave_objects_[i] != null)
            {
                wave_objects_[i].CheckWave();
            }
        }

        // wave更新
        water_.UpdateWave();

        // object height更新
        for (int i = 0; i < wave_objects_.Length; ++i)
        {
            if (wave_objects_[i] != null)
            {
                wave_objects_[i].UpdateHeight();
            }
        }
    }
}
