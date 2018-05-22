The original fruit classification model is taken from Custom Vision iOS sample: https://github.com/xamarin/ios-samples/tree/master/ios11/CoreMLAzureModel

Input: 
[name: "data"
type {
  tensor_type {
    elem_type: FLOAT
    shape {
      dim {
        dim_param: "None"
      }
      dim {
        dim_value: 3
      }
      dim {
        dim_value: 227
      }
      dim {
        dim_value: 227
      }
    }
  }
}
doc_string: "Image(s) in BGR format. It is a [N, C, H, W]-tensor. The 1st/2nd/3rd slices along theC-axis are blue, green, and red channels, respectively."
]

Output: 
[name: "loss"
type {
  map_type {
    key_type: STRING
    value_type {
      tensor_type {
        elem_type: FLOAT
        shape {
          dim {
            dim_value: 1
          }
        }
      }
    }
  }
}
, name: "classLabel"
type {
  tensor_type {
    elem_type: STRING
    shape {
      dim {
        dim_value: 1
      }
    }
  }
}