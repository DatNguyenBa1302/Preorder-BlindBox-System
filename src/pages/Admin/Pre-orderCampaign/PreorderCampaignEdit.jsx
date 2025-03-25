import {
  Button,
  Form,
  Input,
  InputNumber,
  Select,
  DatePicker,
  Space,
} from "antd";
const { Option } = Select;
import { useEffect, useState } from "react";
import { ArrowLeftOutlined } from "@ant-design/icons";
import { Link, useNavigate, useParams } from "react-router";
import TextArea from "antd/es/input/TextArea";
import {
  CreatePreorderCampaign,
  GetActivePreorderCampaignBySlug,
  UpdatePreorderCampaign,
} from "../../../api/Pre_orderCampaign/ApiPre_orderCampaign";
import StatusTag from "../../../components/Tags/StatusTag";
import dayjs from "dayjs";
import isSameOrBefore from "dayjs/plugin/isSameOrBefore";
import { CreatePreorderMilestones } from "../../../components/PreorderMilestones/CreatePreorderMilestones";

const { RangePicker } = DatePicker;

function PreorderCampaignEdit() {
  dayjs.extend(isSameOrBefore);

  const { slug } = useParams();
  const [loading, setLoading] = useState(true);
  const [form] = Form.useForm();
  const [loadMainProduct, setLoadMainProduct] = useState(null);
  const [isUpdated, setIsUpdated] = useState(false);
  const [typeOfCampaign, setTypeOfCampaign] = useState(null);
  const [isFormValid, setIsFormValid] = useState(false);
  const [detailPre_orderCampaign, setDetailPre_orderCampaign] = useState({});
  const [isValidMilestones, setIsValidMilestones] = useState(true);
  const [errorMessageForSelectTime, setErrorMessageForSelectTime] = useState(null);
  const [dateRange, setDateRange] = useState([]);
  const [isAllowed, setIsAllowed] = useState(false);
  const navigate = useNavigate();

  const fetchCampaign_BySlug = async () => {
    try {
      const data = await GetActivePreorderCampaignBySlug(slug);
      setLoadMainProduct(data.blindBox);
      setDetailPre_orderCampaign(data);
      const start = data.startDate ? dayjs(data.startDate, "YYYY-MM-DD HH:mm") : null;
      const end = data.endDate ? dayjs(data.endDate, "YYYY-MM-DD HH:mm") : null;
      setDateRange([start, end]);
      form.setFieldsValue({
        type: data.type === "TimedPricing" ? 0 : 1,
        dateRange: [start, end],
        milestones: data.preorderMilestones,
      });
      onFieldsChange();
      setTypeOfCampaign(data.type);
      setLoading(false);
    } catch (error) {
      console.error("Lỗi khi lấy dữ liệu:", error);
    }
  };

  useEffect(() => {
    fetchCampaign_BySlug();
    if (detailPre_orderCampaign.status !== "Pending") {
      setIsAllowed(false);
    }
  }, [slug]);

  const handleDateChange = (dates) => {
    setDateRange(dates);
    form.setFieldsValue({ dateRange: dates });
    onFieldsChange();
  };

  const onFieldsChange = () => {
    const fieldsError = form.getFieldsError();
    let valid = true;

    if (typeOfCampaign == null || !loadMainProduct) {
      valid = false;
    }

    const milestoneValues = form.getFieldValue("milestones") || [];
    if (milestoneValues.length <= 2) {
      valid = false;
    }
    const milestoneHasEmpty = milestoneValues.some(
      (m) => !m || !m.quantity || !m.price
    );
    if (milestoneHasEmpty) {
      console.log("Empty milestone fields");
      valid = false;
    }

    const currentDate = dayjs();
    const currentDateRange = form.getFieldValue("dateRange");
    if (currentDateRange && currentDateRange.length === 2) {
      const [startDate, endDate] = currentDateRange.map((d) => dayjs(d));
      if (startDate.isSameOrBefore(currentDate)) {
        valid = false;
        setErrorMessageForSelectTime("Ngày bắt đầu bắt buộc phải bắt đầu trong tương lai");
      } else if (endDate.isBefore(startDate.add(5, "day"))) {
        valid = false;
        setErrorMessageForSelectTime("Thời gian hoạt động của chiến dịch không được ít hơn 5 ngày!");
      } else {
        setErrorMessageForSelectTime(null);
      }
    } else {
      valid = false;
    }

    const hasErrors = fieldsError.some((field) => field.errors.length > 0);
    valid = valid && !hasErrors;

    setIsFormValid(valid);
    setIsUpdated(true);
  };

  const handleChangeTypeCampaign = (value) => {
    setTypeOfCampaign(value);
    form.setFieldsValue({ type: value, milestones: [] });
  };

  const handleSubmit = async (values) => {
    console.log(values);
    const data = {
      type: values.type,
      startDate: values.dateRange ? values.dateRange[0].toISOString() : null,
      endDate: values.dateRange ? values.dateRange[1].toISOString() : null,
      preorderMilestoneRequests: (values.milestones || []).map((item) => ({
        quantity: parseInt(item.quantity),
        price: parseFloat(item.price),
      })),
    };

    await UpdatePreorderCampaign(detailPre_orderCampaign.preorderCampaignId, data);
    navigate("/admin/preordercampaign");
  };

  return (
    <div>
      <div className="w-full min-h-screen h-full mx-auto bg-white shadow-lg rounded-lg">
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          onFieldsChange={onFieldsChange}
        >
          <div className="grid grid-cols-12 gap-4 min-h-screen mx-auto mt-5 p-5 bg-[#e5e7eb] shadow-lg rounded-lg">
            <div className="col-span-9">
              <div className="bg-white p-4 rounded-xl py-10">
                <div className="flex items-center mb-4">
                  <Link to="/admin/preordercampaign" className="h-full flex">
                    <ArrowLeftOutlined
                      style={{
                        width: "fit-content",
                        height: "100%",
                        padding: "10px",
                      }}
                      title="Về lại trang danh sách chiến dịch"
                    />
                  </Link>
                  <h1 className="text-2xl font-bold ml-2">
                    Chỉnh sửa chiến dịch
                  </h1>
                  <StatusTag status={detailPre_orderCampaign.status} />
                </div>

                {/* Thông tin sản phẩm */}
                <div className="mb-4">
                  {loadMainProduct && (
                    <div className="grid grid-cols-12 gap-6">
                      <div className="col-span-3">
                        <h3 className="text-[16px]">Hình ảnh sản phẩm</h3>
                        <div className="mt-2">
                          <img
                            src={loadMainProduct.images?.mainImage.url}
                            alt="Main"
                            className="w-full h-[290px] object-cover mt-2 rounded-md"
                          />
                        </div>
                      </div>
                      <div className="col-span-9">
                        <div className="mt-2">
                          <h3 className="text-[16px] mt-2 mb-1">
                            Tên sản phẩm
                          </h3>
                          <Form.Item
                            name="name"
                            initialValue={loadMainProduct?.name}
                          >
                            <Input disabled />
                          </Form.Item>
                          <h3 className="text-[16px] mt-2 mb-1">Mô tả</h3>
                          <Form.Item
                            name="description"
                            initialValue={loadMainProduct?.description}
                          >
                            <TextArea style={{ height: "150px" }} disabled />
                          </Form.Item>
                          <h3 className="text-[16px] mt-2 mb-1">Kích cỡ</h3>
                          <Form.Item
                            name="size"
                            initialValue={loadMainProduct?.size}
                          >
                            <Select disabled className="w-full">
                              <Option value={loadMainProduct?.size}>
                                {loadMainProduct?.size}
                              </Option>
                            </Select>
                          </Form.Item>
                        </div>
                      </div>
                    </div>
                  )}
                </div>

                {/* Milestones */}
                <div>
                  <h2 className="text-xl mb-2 mt-2">
                    Các cột mốc cho chiến dịch
                  </h2>
                  <div className="mb-4">
                    <h3 className="text-lg">Chọn loại chiến dịch</h3>
                    <Form.Item
                      name="type"
                      rules={[
                        {
                          required: true,
                          message: "Vui lòng chọn loại chiến dịch!",
                        },
                      ]}
                    >
                      <Select
                        placeholder="Vui lòng chọn loại chiến dịch"
                        className="w-full"
                        onChange={handleChangeTypeCampaign}
                      >
                        <Option value={0}>Timed Pricing</Option>
                        <Option value={1}>Bulk Order</Option>
                      </Select>
                    </Form.Item>
                  </div>

                  <h3 className="text-lg">Thêm các mốc giá và số lượng</h3>
                  {typeOfCampaign !== null && (
                    <CreatePreorderMilestones
                      setIsValidMilestones={setIsValidMilestones}
                      form={form}
                      typeOfCampaign={typeOfCampaign}
                      loadMainProduct={loadMainProduct}
                    />
                  )}
                </div>
              </div>
            </div>

            {/* Cột bên phải */}
            <div className="col-span-3">
              <div className="py-10 bg-white px-4 rounded-xl">
                <h2 className="text-xl mb-2">Thời gian chiến dịch</h2>
                <Form.Item
                  name="dateRange"
                  label="Chọn khoảng thời gian"
                  rules={[
                    {
                      required: true,
                      message: "Vui lòng chọn khoảng thời gian!",
                    },
                  ]}
                >
                  <RangePicker
                    showTime={{ format: "HH:mm" }}
                    format="YYYY-MM-DD HH:mm"
                    placeholder={['Ngày bắt đầu', 'Ngày kết thúc']}
                    value={dateRange}
                    onChange={handleDateChange}
                  />
                  {errorMessageForSelectTime != null && (
                    <p className="text-red-600">{errorMessageForSelectTime}</p>
                  )}
                </Form.Item>

                <Form.Item>
                  <Button
                    type="primary"
                    htmlType="submit"
                    loading={loading}
                    disabled={!isFormValid || !isValidMilestones}
                    className="w-full"
                  >
                    Cập nhật
                  </Button>
                </Form.Item>
              </div>
            </div>
          </div>
        </Form>
      </div>
    </div>
  );
}

export default PreorderCampaignEdit;
