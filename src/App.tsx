import { useState, useEffect, useRef } from 'react';
import { Check, ArrowLeft, Zap, QrCode, Building2, CreditCard } from 'lucide-react';

const plans = [
  {
    id: 'free',
    name: 'Free',
    price: '0',
    credits: '30 credits',
    description: 'For evaluation and testing',
    features: [
      '30 one-time welcome credits',
      'Virtual Audio Driver integration',
      'Standard TTS voices',
      'Free joining to Pro/Premium rooms'
    ],
    buttonText: 'Start Free',
    popular: false
  },
  {
    id: 'pro',
    name: 'Pro',
    price: '500,000',
    credits: '300 credits/mo',
    description: 'For freelancers and professionals',
    features: [
      'Voice Cloning (original intonation)',
      'Host bi-directional meetings (2 langs)',
      'Real-time subtitles & manual edit',
      'Basic AI meeting summaries'
    ],
    buttonText: 'Upgrade to Pro',
    popular: true
  },
  {
    id: 'premium',
    name: 'Premium',
    price: '1,500,000',
    credits: '1,000 credits/mo',
    description: 'For startups and teams',
    features: [
      'Any-to-Any Multilingual Conferencing',
      'High-fidelity Voice Cloning',
      'Asynchronous AI Meeting Assistant',
      'Workspace admin dashboard'
    ],
    buttonText: 'Upgrade to Premium',
    popular: false
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    price: 'Custom',
    credits: '5,000+ credits/mo',
    description: 'For large organizations',
    features: [
      'Dedicated AI Server Resources',
      'Enterprise Glossary (AI fine-tuning)',
      'API integrations & ROI analytics',
      'Multi-session management'
    ],
    buttonText: 'Contact Sales',
    popular: false
  }
];

export default function App() {
  const [view, setView] = useState<'pricing' | 'checkout_confirm' | 'vnpay_gateway' | 'processing' | 'success'>('pricing');
  const [selectedPlan, setSelectedPlan] = useState<typeof plans[0] | null>(null);

  // Store timeout IDs so they can be securely cleared if user navigates away prematurely
  const timeoutsRef = useRef<ReturnType<typeof setTimeout>[]>([]);

  // Systematic debugging defense: Cleanup effect on unmount or view change
  useEffect(() => {
    return () => {
      // Clear all active timeouts when this component unmounts or `view` changes
      timeoutsRef.current.forEach(clearTimeout);
      timeoutsRef.current = [];
    };
  }, [view]);

  const handleSelectPlan = (plan: typeof plans[0]) => {
    if (plan.id === 'free' || plan.id === 'enterprise') {
      alert(`${plan.name} selected. This would redirect to a different flow in production.`);
      return;
    }
    setSelectedPlan(plan);
    setView('checkout_confirm');
  };

  const simulateRedirectToVnpay = () => {
    // Simulate time taken to redirect to gateway
    const timeout = setTimeout(() => {
      setView('vnpay_gateway');
    }, 800);
    timeoutsRef.current.push(timeout);
  };

  const simulatePaymentProcess = () => {
    setView('processing');
    
    // Simulate payment verification time
    const initialTimeout = setTimeout(() => {
      setView('success');
    }, 2500);

    timeoutsRef.current.push(initialTimeout);
  };

  const renderPricing = () => (
    <div className="app-container">
      <div className="header">
        <h1 className="title">Select your WarpTalk Plan</h1>
        <p className="subtitle">
          Unlock real-time multilingual communication with AI Voice Cloning.
          Thanh toán an toàn qua cổng VNPAY.
        </p>
      </div>

      <div className="pricing-grid">
        {plans.map((plan) => (
          <div key={plan.id} className={`pricing-card ${plan.popular ? 'popular' : ''}`}>
            <div className="card-header">
              <h3 className="tier-name">{plan.name}</h3>
              <div className="tier-price">
                {plan.price === 'Custom' ? 'Custom' : `${plan.price}`}
                {plan.price !== 'Custom' && <span> VND/mo</span>}
              </div>
              <p style={{ color: 'var(--text-secondary)', marginTop: 8 }}>{plan.credits}</p>
            </div>

            <ul className="tier-features">
              {plan.features.map((feature, i) => (
                <li key={i}>
                  <Check className="feature-icon" size={20} />
                  <span>{feature}</span>
                </li>
              ))}
            </ul>

            <button 
              className={`btn ${plan.popular ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => handleSelectPlan(plan)}
            >
              {plan.buttonText}
            </button>
          </div>
        ))}
      </div>
    </div>
  );

  const renderCheckoutConfirm = () => (
    <div className="app-container">
      <div className="checkout-view">
        <div className="checkout-card">
          <div className="checkout-header">
            <button className="back-btn" onClick={() => setView('pricing')}>
              <ArrowLeft size={24} />
            </button>
            <h2 style={{ margin: 0 }}>Xác nhận thanh toán</h2>
          </div>

          <div style={{ textAlign: 'center', marginBottom: '32px' }}>
            <h3 style={{ margin: 0, color: '#64748b' }}>Gói cước {selectedPlan?.name}</h3>
            <div className="amount-display" style={{ margin: '16px 0 8px' }}>{selectedPlan?.price} VND</div>
            <p style={{ color: '#94a3b8', fontSize: '0.9rem' }}>Thanh toán an toàn qua cổng VNPAY</p>
          </div>

          <button className="btn btn-vnpay" onClick={simulateRedirectToVnpay}>
            Thanh toán qua VNPAY
          </button>
        </div>
      </div>
    </div>
  );

  const renderVnpayGateway = () => (
    <div className="vnpay-overlay">
      <div className="vnpay-modal">
        <div className="vnpay-header">
          <div className="vnpay-logo-text">VNPAY<span>QC</span></div>
          <div className="amount-badge">{selectedPlan?.price} VND</div>
        </div>
        
        <div className="vnpay-body">
          <div className="vnpay-title">Chọn phương thức thanh toán</div>
          <div className="vnpay-methods">
            <button className="vnpay-method-btn" onClick={simulatePaymentProcess}>
              <QrCode size={24} color="#005AAB" />
              <span>Ứng dụng thanh toán hỗ trợ VNPAY-QR</span>
            </button>
            <button className="vnpay-method-btn" onClick={simulatePaymentProcess}>
              <Building2 size={24} color="#005AAB" />
              <span>Thẻ nội địa và tài khoản ngân hàng</span>
            </button>
            <button className="vnpay-method-btn" onClick={simulatePaymentProcess}>
              <CreditCard size={24} color="#005AAB" />
              <span>Thẻ thanh toán quốc tế</span>
            </button>
          </div>
          <button className="vnpay-cancel" onClick={() => setView('pricing')}>
            Hủy thanh toán
          </button>
        </div>
      </div>
    </div>
  );

  const renderProcessing = () => (
    <div className="app-container" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '60vh' }}>
      <div style={{ textAlign: 'center' }}>
        <div className="loader" style={{ borderColor: 'rgba(59, 130, 246, 0.3)', borderTopColor: '#3b82f6', width: 48, height: 48, borderWidth: 4, marginBottom: 24 }} />
        <h2 style={{ color: 'var(--text-primary)', marginBottom: 8 }}>Đang giao dịch...</h2>
        <p style={{ color: 'var(--text-secondary)' }}>Vui lòng không đóng trình duyệt</p>
      </div>
    </div>
  );

  const renderSuccess = () => (
    <div className="app-container" style={{ textAlign: 'center', paddingTop: '100px' }}>
      <div className="success-icon">
        <Zap size={40} />
      </div>
      <h1 className="title" style={{ fontSize: '2.5rem' }}>Chào mừng đến với {selectedPlan?.name}!</h1>
      <p className="subtitle" style={{ marginBottom: '32px' }}>
        Tài khoản của bạn đã được nâng cấp thành công. Bạn hiện có {selectedPlan?.credits}.
      </p>
      <button className="btn btn-primary" style={{ maxWidth: '200px' }} onClick={() => setView('pricing')}>
        Quay về Dashboard
      </button>
    </div>
  );

  return (
    <>
      {view === 'pricing' && renderPricing()}
      {view === 'checkout_confirm' && renderCheckoutConfirm()}
      {view === 'vnpay_gateway' && renderVnpayGateway()}
      {view === 'processing' && renderProcessing()}
      {view === 'success' && renderSuccess()}
    </>
  );
}
