using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterCsController : MonoBehaviour
{
    [SerializeField] ComputeShader compute_shader_;
    [SerializeField, Range(0f, 100f)] float wave_speed_ = 5f;
    private static readonly int kVertexNumberX = 128;
    private static readonly int kVertexNumberY = 128;
    private static readonly float kWaveHeightMultiplyer = 1.5f;
    private int start_wave_kernel_index_;
    private int update_wave_kernel_index_;
    private int update_polygon_normal_kernel_index_;
    private int update_vertex_normal_kernel_index_;
    private RenderTexture render_texture_;
    private ComputeBuffer[] compute_buffer_ = new ComputeBuffer[5]; // 0 - 2 : 前フレーム、今フレーム、次フレーム高さ
                                                                    // 3 : 頂点位置xy 
                                                                    // 4 : 法線
    private float[] current_heights_ = new float[kVertexNumberX * kVertexNumberY];
    private int group_size_x_, group_size_y_;
    private int current_buffer_count_ = 0;
    private Material material_;

    public void OccurWave(Transform test_object, Vector3 direction)
    {
        Vector3 object_position = test_object.position;
        Vector2 half_size = new Vector2(transform.localScale.x, transform.localScale.z) * 0.5f;
        if (object_position.x <= -half_size.x || object_position.x >= half_size.x
            || object_position.z <= -half_size.y || object_position.z >= half_size.y)
        {
            return;
        }

        int[] wave_position = new int[2];
        wave_position[0] = (int)((object_position.x + half_size.x) / (half_size.x * 2f) * kVertexNumberX - direction.x);
        wave_position[1] = (int)((-object_position.z + half_size.y) / (half_size.y * 2f) * kVertexNumberY + direction.z);

        // 高さを判定する
        float obj_height = object_position.y - test_object.localScale.y * 0.5f;
        float wave_height = current_heights_[wave_position[1] * kVertexNumberX + wave_position[0]] * kWaveHeightMultiplyer;
        if (obj_height > wave_height)
        {
            return;
        }

        compute_shader_.SetBuffer(start_wave_kernel_index_, "current_buffer", compute_buffer_[current_buffer_count_]);
        compute_shader_.SetInts("wave_position", wave_position);
        compute_shader_.SetFloat("wave_value", (obj_height + test_object.localScale.y * 0.25f) / kWaveHeightMultiplyer);
        compute_shader_.Dispatch(start_wave_kernel_index_, 1, 1, 1);
    }

    public float ReturnHeight(Vector3 object_position)
    {
        Vector2 half_size = new Vector2(transform.localScale.x, transform.localScale.z) * 0.5f;
        if (object_position.x <= -half_size.x || object_position.x >= half_size.x
            || object_position.z <= -half_size.y || object_position.z >= half_size.y)
        {
            return 0f;
        }

        int[] wave_position = new int[2];
        wave_position[0] = (int)((object_position.x + half_size.x) / (half_size.x * 2f) * kVertexNumberX);
        wave_position[1] = (int)((-object_position.z + half_size.y) / (half_size.y * 2f) * kVertexNumberY);
        return current_heights_[wave_position[1] * kVertexNumberX + wave_position[0]] * kWaveHeightMultiplyer;
    }

    public void UpdateWave()
    {
        // 直前、直後の頂点取得
        int previous = (current_buffer_count_ + 2) % 3;
        int next = (current_buffer_count_ + 1) % 3;

        // 波の速度、時間の刻み、グリッド幅
        float h = 1.0f;
        float cth = (wave_speed_ * wave_speed_ * Time.deltaTime * Time.deltaTime) / (h * h);
        cth = Mathf.Clamp(cth, 0f, 0.5f);

        // 波の更新
        compute_shader_.SetBuffer(update_wave_kernel_index_, "previous_buffer", compute_buffer_[previous]);
        compute_shader_.SetBuffer(update_wave_kernel_index_, "current_buffer", compute_buffer_[current_buffer_count_]);
        compute_shader_.SetBuffer(update_wave_kernel_index_, "next_buffer", compute_buffer_[next]);
        compute_shader_.SetFloat("cth", cth);
        compute_shader_.Dispatch(update_wave_kernel_index_, group_size_x_, group_size_y_, 1);

        // 参照座標を更新
        current_buffer_count_ = next;

        // 面法線更新
        compute_shader_.SetBuffer(update_polygon_normal_kernel_index_, "current_buffer", compute_buffer_[current_buffer_count_]);
        compute_shader_.Dispatch(update_polygon_normal_kernel_index_, group_size_x_, group_size_y_, 1);

        // 頂点法線更新
        compute_shader_.SetBuffer(update_vertex_normal_kernel_index_, "current_buffer", compute_buffer_[current_buffer_count_]);
        compute_shader_.Dispatch(update_vertex_normal_kernel_index_, group_size_x_, group_size_y_, 1);

        material_.SetTexture("_BumpHeightMap", render_texture_);

        // 今の頂点高さを取得
        compute_buffer_[current_buffer_count_].GetData(current_heights_);
    }

    // Use this for initialization
    private void Start()
    {
        InitBuffer();
        InitWave();
        InitComputeShader();
        material_ = GetComponent<Renderer>().material;
        WaveManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        render_texture_.Release();
        for (int i = 0; i < 5; ++i)
        {
            compute_buffer_[i].Release();
        }
    }

    private void InitBuffer()
    {
        render_texture_ = new RenderTexture(kVertexNumberX, kVertexNumberY, 0, RenderTextureFormat.ARGB32);
        render_texture_.wrapMode = TextureWrapMode.Clamp;
        render_texture_.enableRandomWrite = true;
        render_texture_.Create();

        for (int i = 0; i < 3; ++i)
        {
            compute_buffer_[i] = new ComputeBuffer(kVertexNumberX * kVertexNumberY, sizeof(float));
        }
        compute_buffer_[3] = new ComputeBuffer(kVertexNumberX * kVertexNumberY, sizeof(float) * 2);
        compute_buffer_[4] = new ComputeBuffer(kVertexNumberX * kVertexNumberY * 2, sizeof(float) * 3);
    }

    private void InitWave()
    {
        uint thread_size_x, thread_size_y, thread_size_z;
        int kernel_index = compute_shader_.FindKernel("CSInitWave");
        compute_shader_.GetKernelThreadGroupSizes(kernel_index, out thread_size_x, out thread_size_y, out thread_size_z);

        int previous = (current_buffer_count_ + 2) % 3;
        int next = (current_buffer_count_ + 1) % 3;
        compute_shader_.SetBuffer(kernel_index, "previous_buffer", compute_buffer_[previous]);
        compute_shader_.SetBuffer(kernel_index, "current_buffer", compute_buffer_[current_buffer_count_]);
        compute_shader_.SetBuffer(kernel_index, "next_buffer", compute_buffer_[next]);
        compute_shader_.SetBuffer(kernel_index, "vertex_buffer", compute_buffer_[3]);

        int[] vertex_number = new int[2];
        vertex_number[0] = kVertexNumberX;
        vertex_number[1] = kVertexNumberY;
        compute_shader_.SetInts("vertex_number", vertex_number);

        int group_size_x = kVertexNumberX / (int)thread_size_x;
        int group_size_y = kVertexNumberY / (int)thread_size_y;
        compute_shader_.Dispatch(kernel_index, group_size_x, group_size_y, (int)thread_size_z);
    }

    private void InitComputeShader()
    {
        start_wave_kernel_index_ = compute_shader_.FindKernel("CSStartWave");

        update_wave_kernel_index_ = compute_shader_.FindKernel("CSUpdateHeight");
        compute_shader_.SetBuffer(update_wave_kernel_index_, "vertex_buffer", compute_buffer_[3]);
        compute_shader_.SetBuffer(update_wave_kernel_index_, "normal_buffer", compute_buffer_[4]);

        update_polygon_normal_kernel_index_ = compute_shader_.FindKernel("CSUpdatePolygonNormal");
        compute_shader_.SetBuffer(update_polygon_normal_kernel_index_, "vertex_buffer", compute_buffer_[3]);
        compute_shader_.SetBuffer(update_polygon_normal_kernel_index_, "normal_buffer", compute_buffer_[4]);

        update_vertex_normal_kernel_index_ = compute_shader_.FindKernel("CSUpdateVertexNormal");
        compute_shader_.SetBuffer(update_vertex_normal_kernel_index_, "normal_buffer", compute_buffer_[4]);
        compute_shader_.SetTexture(update_vertex_normal_kernel_index_, "bump_height_map", render_texture_);

        // グループ数は「テクスチャの水平(垂直)方向の解像度 / 水平(垂直)方向のスレッド数」で算出しています
        uint thread_size_x, thread_size_y, thread_size_z;
        compute_shader_.GetKernelThreadGroupSizes(update_wave_kernel_index_, out thread_size_x, out thread_size_y, out thread_size_z);
        group_size_x_ = kVertexNumberX / (int)thread_size_x;
        group_size_y_ = kVertexNumberY / (int)thread_size_y;
    }
}