// Toggle search bar
document.getElementById('searchToggle').addEventListener('click', function () {
    const searchContainer = document.getElementById('searchContainer');
    searchContainer.classList.toggle('expanded');

    // Focus on input when expanded
    if (searchContainer.classList.contains('expanded')) {
        setTimeout(() => {
            document.querySelector('.search-box').focus();
        }, 300);
    }
});

// Close search when clicking outside
document.addEventListener('click', function (e) {
    const searchContainer = document.getElementById('searchContainer');
    if (searchContainer.classList.contains('expanded') &&
        !searchContainer.contains(e.target) &&
        e.target !== document.getElementById('searchToggle')) {
        searchContainer.classList.remove('expanded');
    }
});

// Hàm hỗ trợ thao tác với sản phẩm
function buyNow(productId) {
    alert(`Mua ngay sản phẩm ${productId}`);
}

function addToCart(productId) {
    alert(`Đã thêm sản phẩm ${productId} vào giỏ hàng`);
}

function viewProduct(productId) {
    alert(`Xem chi tiết sản phẩm ${productId}`);
}

// Hiệu ứng cuộn mượt cho các liên kết
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        document.querySelector(this.getAttribute('href')).scrollIntoView({
            behavior: 'smooth'
        });
    });
});

document.getElementById('chatIcon').addEventListener('click', function () {
    alert('Mở hộp thoại chat');
});

// Kiểm tra tin nhắn chưa đọc (đơn giản)
function checkUnreadMessages() {
    // Giả lập có 3 tin nhắn chưa đọc
    document.getElementById('unreadCount').style.display = 'block';
    document.getElementById('unreadCount').textContent = '3';
}

// Kiểm tra mỗi 30 giây
setInterval(checkUnreadMessages, 30000);
checkUnreadMessages();

// Form validation for newsletter
document.querySelector('.newsletter-form').addEventListener('submit', function (e) {
    e.preventDefault();
    const email = this.querySelector('input[type="email"]').value;
    if (email && email.includes('@')) {
        alert(`Cảm ơn bạn đã đăng ký với email: ${email}`);
        this.reset();
    } else {
        alert('Vui lòng nhập email hợp lệ');
    }
});