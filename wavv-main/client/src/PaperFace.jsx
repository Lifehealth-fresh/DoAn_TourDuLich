import { useEffect, useState } from 'react';
import { paperImageBlob } from './api';

export default function PaperFace({ khachId, giayToId, mat, alt }) {
  const [src, setSrc] = useState('');
  useEffect(() => {
    let url = '';
    let alive = true;
    paperImageBlob(khachId, giayToId, mat)
      .then((response) => {
        if (!alive) return;
        url = URL.createObjectURL(response.data);
        setSrc(url);
      })
      .catch(() => { if (alive) setSrc(''); });
    return () => {
      alive = false;
      if (url) URL.revokeObjectURL(url);
    };
  }, [khachId, giayToId, mat]);
  if (!src) return null;
  return <img src={src} alt={alt || mat} style={{ height: 72 }} />;
}
