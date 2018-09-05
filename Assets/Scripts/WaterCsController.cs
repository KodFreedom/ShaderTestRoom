using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterCsController : MonoBehaviour
{
    // 構造体
    private struct Block
    {
        public Material material;
        public Transform transform;
    }

    // 定数
    private static readonly int kWaterBlockNumberSide = 3;
    private static readonly int kVertexNumberSide = 128;
    private static readonly int kTotalWaterBlockNumber = kWaterBlockNumberSide * kWaterBlockNumberSide;
    private static readonly int kTotalVertexNumber = kVertexNumberSide * kVertexNumberSide;
    private static readonly float kWaveHeightMultiplyer = 1.5f;

    // 変数
    [SerializeField] ComputeShader compute_shader_;
    //[SerializeField, Range(0f, 100f)] float wave_speed_ = 5f;
    private int[] block_indeces_ = new int[kTotalWaterBlockNumber];
    private int occur_wave_kernel_;
    private int update_wave_kernel_;
    private int update_polygon_normal_kernel_;
    private int update_vertex_normal_kernel_;
    private int group_size_x_, group_size_y_;
    private int current_buffer_count_ = 0;
    private float[] current_heights_ = new float[kTotalVertexNumber * kTotalWaterBlockNumber];
    private Block[] blocks_ = new Block[kTotalWaterBlockNumber];
    private RenderTexture render_texture_;
    private ComputeBuffer[] height_buffers_ = new ComputeBuffer[3]; // 前フレーム、今フレーム、次フレーム高さ
    private ComputeBuffer polygon_normal_buffer_; // 面法線
    private ComputeBuffer block_indeces_buffer_;

    public void OccurWave(Transform test_object, Vector3 direction)
    {
        Vector3 object_position = test_object.position;
        int center = block_indeces_[4];
        var center_position = blocks_[center].transform.position;
        object_position = object_position - center_position;
        float half_size = transform.localScale.x * 0.5f;

        if (object_position.x <= -half_size || object_position.x >= half_size
            || object_position.z <= -half_size || object_position.z >= half_size)
        {
            // TODO : 敵なら消滅する
            return;
        }

        int[] wave_position = new int[2];
        wave_position[0] = (center % 3) * kVertexNumberSide + (int)((object_position.x + half_size) / (half_size * 2f) * kVertexNumberSide - direction.x);
        wave_position[1] = (center / 3) * kVertexNumberSide + (int)((-object_position.z + half_size) / (half_size * 2f) * kVertexNumberSide + direction.z);

        // 高さを判定する
        float obj_height = object_position.y - test_object.localScale.y * 0.5f;
        float wave_height = current_heights_[wave_position[1] * kVertexNumberSide + wave_position[0]] * kWaveHeightMultiplyer;
        if (obj_height > wave_height)
        {
            return;
        }

        compute_shader_.SetBuffer(occur_wave_kernel_, "current_height_buffer", height_buffers_[current_buffer_count_]);
        compute_shader_.SetInts("wave_position", wave_position);
        compute_shader_.SetFloat("wave_value", (obj_height + test_object.localScale.y * 0.25f) / kWaveHeightMultiplyer);
        compute_shader_.Dispatch(occur_wave_kernel_, 1, 1, 1);
    }

    public float ReturnHeight(Vector3 object_position)
    {
        return 0f;
        //Vector2 half_size = new Vector2(transform.localScale.x, transform.localScale.z) * 0.5f;
        //if (object_position.x <= -half_size || object_position.x >= half_size
        //    || object_position.z <= -half_size || object_position.z >= half_size)
        //{
        //    return 0f;
        //}

        //int[] wave_position = new int[2];
        //wave_position[0] = (int)((object_position.x + half_size) / (half_size * 2f) * kVertexNumber);
        //wave_position[1] = (int)((-object_position.z + half_size) / (half_size * 2f) * kVertexNumber);
        //return current_heights_[wave_position[1] * kVertexNumber + wave_position[0]] * kWaveHeightMultiplyer;
    }

    /// <summary>
    /// 波の更新
    /// </summary>
    public void UpdateWave(Vector3 player_position)
    {
        // Blockの更新
        UpdateBlocks(player_position);

        // 最新のindexを設定
        block_indeces_buffer_.SetData(block_indeces_);

        // 直前、直後の頂点取得
        int previous = (current_buffer_count_ + 2) % 3;
        int next = (current_buffer_count_ + 1) % 3;

        // 波の速度、時間の刻み、グリッド幅
        //float h = 1.0f;
        //float cth = (wave_speed_ * wave_speed_ * Time.deltaTime * Time.deltaTime) / (h * h);
        //cth = Mathf.Clamp(cth, 0f, 0.5f);
        float c = 0.4f;
        float dt = 0.4f;
        float h = 1.0f;
        float cth = (c * c * dt * dt) / (h * h);
        compute_shader_.SetFloat("cth", cth);

        // 波の更新
        compute_shader_.SetBuffer(update_wave_kernel_, "previous_height_buffer", height_buffers_[previous]);
        compute_shader_.SetBuffer(update_wave_kernel_, "current_height_buffer", height_buffers_[current_buffer_count_]);
        compute_shader_.SetBuffer(update_wave_kernel_, "next_height_buffer", height_buffers_[next]);
        compute_shader_.Dispatch(update_wave_kernel_, group_size_x_, group_size_y_, 1);

        // 参照座標を更新
        current_buffer_count_ = next;

        // 面法線更新
        compute_shader_.SetBuffer(update_polygon_normal_kernel_, "current_height_buffer", height_buffers_[current_buffer_count_]);
        compute_shader_.Dispatch(update_polygon_normal_kernel_, group_size_x_, group_size_y_, 1);

        // 頂点法線更新
        compute_shader_.SetBuffer(update_vertex_normal_kernel_, "current_height_buffer", height_buffers_[current_buffer_count_]);
        compute_shader_.Dispatch(update_vertex_normal_kernel_, group_size_x_, group_size_y_, 1);

        // 今の頂点高さを取得
        height_buffers_[current_buffer_count_].GetData(current_heights_);
    }

    // 初期化
    private void Start()
    {
        InitBuffer();
        InitBlocks();
        InitComputeShader();
        InitWave();
        
        WaveManager.Instance.Register(this);
    }

    // 破棄
    private void OnDestroy()
    {
        render_texture_.Release();
        polygon_normal_buffer_.Release();
        for(int i = 0; i < 3; ++i)
        {
            height_buffers_[i].Release();
        }
        block_indeces_buffer_.Release();
    }

    // バッファの初期化
    private void InitBuffer()
    {
        render_texture_ = new RenderTexture(kVertexNumberSide, kVertexNumberSide, 0, RenderTextureFormat.ARGB32);
        render_texture_.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        render_texture_.volumeDepth = kTotalWaterBlockNumber;
        render_texture_.wrapMode = TextureWrapMode.Clamp;
        render_texture_.enableRandomWrite = true;
        render_texture_.Create();

        polygon_normal_buffer_ = new ComputeBuffer(kTotalVertexNumber * kTotalWaterBlockNumber * 2, sizeof(float) * 3);
        for (int i = 0; i < 3; ++i)
        {
            height_buffers_[i] = new ComputeBuffer(kTotalVertexNumber * kTotalWaterBlockNumber, sizeof(float));
        }
        block_indeces_buffer_ = new ComputeBuffer(kTotalWaterBlockNumber, sizeof(int));
    }

    // ブロックの初期化
    private void InitBlocks()
    {
        for(int i = 0; i < kTotalWaterBlockNumber; ++i)
        {
            block_indeces_[i] = i;
            blocks_[i].transform = transform.Find("WaterBlock" + i);
            blocks_[i].material = blocks_[i].transform.GetComponent<Renderer>().material;
            blocks_[i].material.SetInt("_TextureZ", i);
            blocks_[i].material.SetTexture("_BumpHeightMaps", render_texture_);
        }
    }

    // compute shaderの初期化
    private void InitComputeShader()
    {
        occur_wave_kernel_ = compute_shader_.FindKernel("CSStartWave");
        compute_shader_.SetBuffer(occur_wave_kernel_, "block_indeces", block_indeces_buffer_);

        update_wave_kernel_ = compute_shader_.FindKernel("CSUpdateHeight");
        compute_shader_.SetBuffer(update_wave_kernel_, "block_indeces", block_indeces_buffer_);
        compute_shader_.SetBuffer(update_wave_kernel_, "polygon_normal_buffer", polygon_normal_buffer_);

        update_polygon_normal_kernel_ = compute_shader_.FindKernel("CSUpdatePolygonNormal");
        compute_shader_.SetBuffer(update_polygon_normal_kernel_, "block_indeces", block_indeces_buffer_);
        compute_shader_.SetBuffer(update_polygon_normal_kernel_, "polygon_normal_buffer", polygon_normal_buffer_);

        update_vertex_normal_kernel_ = compute_shader_.FindKernel("CSUpdateVertexNormal");
        compute_shader_.SetBuffer(update_vertex_normal_kernel_, "block_indeces", block_indeces_buffer_);
        compute_shader_.SetBuffer(update_vertex_normal_kernel_, "polygon_normal_buffer", polygon_normal_buffer_);
        compute_shader_.SetTexture(update_vertex_normal_kernel_, "bump_height_maps", render_texture_);
        
        uint thread_size_x, thread_size_y, thread_size_z;
        compute_shader_.GetKernelThreadGroupSizes(update_wave_kernel_, out thread_size_x, out thread_size_y, out thread_size_z);
        group_size_x_ = kVertexNumberSide * kWaterBlockNumberSide / (int)thread_size_x;
        group_size_y_ = kVertexNumberSide * kWaterBlockNumberSide / (int)thread_size_y;
        Debug.Log("group size x : " + group_size_x_);
        Debug.Log("group size y : " + group_size_y_);
    }

    // 波の初期化
    private void InitWave()
    {
        uint thread_size_x, thread_size_y, thread_size_z;
        int kernel_index = compute_shader_.FindKernel("CSInitWave");
        compute_shader_.GetKernelThreadGroupSizes(kernel_index, out thread_size_x, out thread_size_y, out thread_size_z);
        int group_size_x = kVertexNumberSide * kWaterBlockNumberSide / (int)thread_size_x;
        int group_size_y = kVertexNumberSide * kWaterBlockNumberSide / (int)thread_size_y;
        
        compute_shader_.SetInt("vertex_number_per_block", kVertexNumberSide);
        compute_shader_.SetBuffer(kernel_index, "previous_height_buffer", height_buffers_[0]);
        compute_shader_.SetBuffer(kernel_index, "current_height_buffer", height_buffers_[1]);
        compute_shader_.SetBuffer(kernel_index, "next_height_buffer", height_buffers_[2]);

        compute_shader_.Dispatch(kernel_index, group_size_x, group_size_y, 1);
    }

    // プレイヤーの位置に応じてブロックを切り替える
    private void UpdateBlocks(Vector3 player_position)
    {
        // 中央のブロックにいるかどうかをチェック
        // 0 1 2
        // 3 4 5
        // 6 7 8
        int center = block_indeces_[4];
        var center_position = blocks_[center].transform.position;
        var real_player_position = player_position - center_position;
        float half_size = transform.localScale.x * 0.5f;

        // いないなら上下左右に応じてブロックの更新を行う
        if (real_player_position.x < -half_size)
        {// 左、258を初期化して左列に移す
            ResetBlocks(block_indeces_[2], block_indeces_[5], block_indeces_[8], Vector3.left * 3f);
            ShiftBlockX(2);
        }
        else if (real_player_position.x > half_size)
        {// 右、036を初期化して右列に移す
            ResetBlocks(block_indeces_[0], block_indeces_[3], block_indeces_[6], Vector3.right * 3f);
            ShiftBlockX(1);
        }
        else if (real_player_position.z < -half_size)
        {// 下、012を初期化して下行に移す
            ResetBlocks(block_indeces_[0], block_indeces_[1], block_indeces_[2], Vector3.back * 3f);
            ShiftBlockY(1);
        }
        else if (real_player_position.z > half_size)
        {// 上、678を初期化して上行に移す
            ResetBlocks(block_indeces_[6], block_indeces_[7], block_indeces_[8], Vector3.forward * 3f);
            ShiftBlockY(2);
        }
    }

    // ブロックの位置をずらす
    private void ResetBlocks(int index0, int index1, int index2, Vector3 offset)
    {
        int[] indeces = new int[3];
        indeces[0] = index0;
        indeces[1] = index1;
        indeces[2] = index2;

        for(int i = 0; i < 3; ++i)
        {
            blocks_[indeces[i]].transform.localPosition += offset;
        }
    }

    // 列ごとシフトする
    private void ShiftBlockX(int shift_value)
    {
        int[] copy = new int[kTotalWaterBlockNumber];
        block_indeces_.CopyTo(copy, 0);
        for (int i = 0; i < 3; ++i)
        {
            block_indeces_[i + 0] = copy[(i + shift_value) % 3 + 0];
            block_indeces_[i + 3] = copy[(i + shift_value) % 3 + 3];
            block_indeces_[i + 6] = copy[(i + shift_value) % 3 + 6];
        }
    }

    // 行ごとシフトする
    private void ShiftBlockY(int shift_value)
    {
        int[] copy = new int[kTotalWaterBlockNumber];
        block_indeces_.CopyTo(copy, 0);
        for (int i = 0; i < 3; ++i)
        {
            block_indeces_[i * 3 + 0] = copy[(i + shift_value) % 3 * 3 + 0];
            block_indeces_[i * 3 + 1] = copy[(i + shift_value) % 3 * 3 + 1];
            block_indeces_[i * 3 + 2] = copy[(i + shift_value) % 3 * 3 + 2];
        }
    }
}