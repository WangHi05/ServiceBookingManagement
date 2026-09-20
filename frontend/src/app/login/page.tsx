import { Suspense } from 'react';
import { LoginForm } from './LoginForm';
import { LoadingState } from '@/components/LoadingState';

export default function LoginPage() {
  return (
    <Suspense fallback={<LoadingState />}>
      <LoginForm />
    </Suspense>
  );
}
