import React from 'react';

export function VoucherModal({
  vouchers,
  inputVoucherCode,
  setInputVoucherCode,
  handleApplyInputVoucher,
  applyVoucherFromList,
  onClose,
}) {
  return (
    <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-50 z-50">
      <div className="bg-white p-4 rounded shadow max-w-sm w-full">
        <h2 className="text-xl font-bold mb-4">Chọn hoặc nhập mã</h2>

        {/* Khu vực nhập mã voucher */}
        <div className="flex items-center mb-4 space-x-2">
          <input
            type="text"
            placeholder="Coupon Code"
            value={inputVoucherCode}
            onChange={(e) => setInputVoucherCode(e.target.value)}
            className="flex-grow px-2 py-1 border border-gray-300 rounded"
          />
          <button
            className="bg-yellow-500 text-white px-4 py-1 rounded"
            onClick={handleApplyInputVoucher}
          >
            APPLY
          </button>
        </div>

        {/* Danh sách voucher có sẵn */}
        <ul className="space-y-2">
          {vouchers.map((voucher) => (
            <li
              key={voucher.id}
              className="flex items-center justify-between p-2 border rounded"
            >
              <div>
                <p className="font-semibold">{voucher.title}</p>
                <p className="text-sm text-gray-600">{voucher.description}</p>
              </div>
              <button
                className="bg-yellow-500 text-white px-3 py-1 rounded"
                onClick={() => applyVoucherFromList(voucher.id)}
              >
                Chọn
              </button>
            </li>
          ))}
        </ul>

        {/* Nút đóng modal */}
        <button
          className="mt-4 bg-gray-200 text-gray-800 px-4 py-2 rounded uppercase"
          onClick={onClose}
        >
          Đóng
        </button>
      </div>
    </div>
  );
}
