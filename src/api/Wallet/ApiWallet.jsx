import api from "../instance";

const ApiGetWalletInfomation = async () => {
   try {
      const result = await api.get('/Wallet')
      if (result.status === 200) {
         return result.data;
      }
   } catch (error) {
      console.log('Api Get Wallet Infomation Error: ', error);
   }
}

const ApiDepositByMono = async ({ payload }) => {
   try {
      const response = await api.post('/Wallet/CreatePaymentUrlMomo', payload);
      if (response.status === 200) {
         return response.data;
      }
   } catch (error) {
      console.log('Api Deposit By Mono Error: ', error);
   }
}

const ApiDepositByVnpay = async ({ payload }) => {
   try {
      const response = await api.post('/Wallet/CreatePaymentUrlVnpay', payload);
      if (response.status === 200) {
         return response.data;
      }
   } catch (error) {
      console.log('Api Deposit By Vnpay Error: ', error);
   }
}

export { ApiGetWalletInfomation, ApiDepositByMono, ApiDepositByVnpay }