using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestCsController : MonoBehaviour
{
    [SerializeField] ComputeShader compute_shader_;
    private static readonly int kBufferNumber = 4;
    private int kernel_index_a_;
    private int kernel_index_b_;
    private ComputeBuffer compute_buffer_;
    private int[] result_ = new int[kBufferNumber];
    private Text output_text_;

    // Use this for initialization
    private void Start ()
    {
        kernel_index_a_ = compute_shader_.FindKernel("CSMainA");
        kernel_index_b_ = compute_shader_.FindKernel("CSMainB");
        compute_buffer_ = new ComputeBuffer(kBufferNumber, sizeof(int));
        compute_shader_.SetBuffer(kernel_index_a_, "int_buffer", compute_buffer_);
        compute_shader_.SetBuffer(kernel_index_b_, "int_buffer", compute_buffer_);
        compute_shader_.SetInt("int_value", 1);
        output_text_ = GetComponent<Text>();
	}

    // Update is called once per frame
    private void Update ()
    {
        compute_shader_.Dispatch(kernel_index_a_, 1, 1, 1);
        //compute_shader_.Dispatch(kernel_index_b_, 1, 1, 1);
        compute_buffer_.GetData(result_);
        output_text_.text = "";
        for (int i = 0; i < kBufferNumber; ++i)
        {
            output_text_.text += result_[i].ToString() + " , ";
        }
	}

    private void OnDestroy()
    {
        compute_buffer_.Release();
    }
}
