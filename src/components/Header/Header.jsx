import { ChevronDown, LogOut, Search, ShoppingCart, User, Wallet } from "lucide-react"
import { Link } from "react-router-dom"
import { useContext, useEffect } from "react"

import logo from "../../assets/Header/logo.png"
import { AuthContext } from "../../context/AuthContext"
import SearchImage from '../../assets/SearchInHeader/SanPham.jpg'
import SearchInputInHeader from "../SearchCampaign/SearchInputInHeader"
import { useCart } from "../../context/CartContext"
import useLogout from "../../hooks/useLogout"

export default function Header() {

    const { auth } = useContext(AuthContext)
    const { cartData, CallGetAllCart } = useCart()
    const logout = useLogout();

    console.log(auth);

    const handleLogout = () => {
        console.log('Nhan logout');
        logout()
    }

    useEffect(() => {
        if (auth.roleName.toLowerCase() === 'customer') {
            CallGetAllCart()
        }
    }, [])

    return (
        <header className="sticky top-0 z-50 w-full border-b bg-white">
            <div className="container-lg mx-auto flex h-16 items-center justify-between !p-3">
                <Link to="/" className="flex items-center w-[16%] pl-[2rem]">
                    <img
                        src={logo}
                        alt="Vaithuhay.com Logo"
                        className="h-10 w-auto"
                    />
                </Link>
                <nav className="w-[50%]">
                    <div className="hidden md:flex space-x-8 items-center float-right">
                        <Link to="/" className="text-[15px] font-medium hover:text-gray-600 flex items-center px-[20px] py-[10px] leading-[20px] ">
                            <p className=" text-[#333] text-[16px]">Trang chủ</p> <ChevronDown className="w-4 h-4" />
                        </Link>
                        <div className="relative group">
                            <Link to="/gioi-thieu" className="text-[15px] font-medium hover:text-gray-600 flex items-center px-[20px] py-[10px] leading-[20px]">
                                <p className=" text-[#333] text-[16px]">Giới thiệu</p> <ChevronDown className="w-4 h-4" />
                            </Link>
                        </div>
                        <div className="relative group">
                            <Link to="/san-pham" className="text-[15px] font-medium hover:text-gray-600 flex items-center px-[20px] py-[10px] leading-[20px]">
                                <p className=" text-[#333] text-[16px]">Sản phẩm</p> <ChevronDown className="w-4 h-4" />
                            </Link>
                        </div>
                        <div className="relative group">
                            <Link to="/bai-viet" className="text-[15px] font-medium hover:text-gray-600 flex items-center px-[20px] py-[10px] leading-[20px]">
                                <p className=" text-[#333] text-[16px]">Bài viết</p> <ChevronDown className="w-4 h-4" />
                            </Link>
                        </div>
                    </div>
                </nav>

                <div className="w-[33%]">
                    <div className="float-right flex items-center space-x-4">

                        <SearchInputInHeader />

                        {auth.roleName.toLowerCase() === 'customer' ? (
                            <>
                                <button className="relative px-[5px]">
                                    <Link to='/cart'>
                                        <ShoppingCart className="h-6 w-6" />
                                        <span className={`${cartData?.length == 0 || cartData?.length == undefined ? 'hidden' : ''} absolute -right-2 -top-2 flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-xs text-white`}>
                                            {cartData?.length}
                                        </span>
                                    </Link>
                                </button>

                                <button className="h-10 px-[5px]">
                                    <Link to='/wallet'>
                                        <Wallet />
                                    </Link>
                                </button>

                                <div className="px-[1rem] group h-10 flex items-center">
                                    <User className="h-6 w-6" />
                                    <div className="absolute top-[80%] right-[10px] hidden group-hover:block">
                                        <div className="mt-[15px] min-w-[16rem] bg-[#fff] rounded-[8px] shadow-[0_0_0_1px_#d1d2e0,0_2px_4px_rgba(6,17,118,0.08),0_4px_12px_rgba(6,17,118,0.08)]">
                                            <ul className="py-[8px] ">
                                                <li onClick={handleLogout} className="cursor-pointer py-[8px] hover:bg-[#ccc] px-[16px] flex items-start w-full h-auto text-left whitespace-normal text-[16px]">
                                                    Đăng xuất
                                                </li>
                                                <Link to="/myvoucher">
                                                    <li className="cursor-pointer py-[8px] hover:bg-[#ccc] px-[16px] flex items-start w-full h-auto text-left whitespace-normal text-[16px]">
                                                        Phiếu giảm giá
                                                    </li>
                                                </Link>
                                            </ul>
                                        </div>
                                    </div>
                                </div>
                            </>
                        ) : (
                            <>
                                <Link to='/login' className="pl-[10px] py-[10px] ">
                                    Đăng nhập
                                </Link>
                                <Link to='/register' className="px-[10px] py-[10px] border-solid border-l-[1px] border-[#e5e5e5]">
                                    Đăng ký
                                </Link>
                            </>
                        )}

                    </div>
                </div>
            </div>
        </header>
    )
}

